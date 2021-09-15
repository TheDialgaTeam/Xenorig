// Xirorig
// Copyright 2021 Yong Jian Ming
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Xirorig.Algorithms.Xiropht.Decentralized;
using Xirorig.Miner.Network.Api.JobResult;
using Xirorig.Miner.Network.Api.JobTemplate;
using Xirorig.Miner.Network.Api.JsonConverter;
using Xirorig.Options;
using Xirorig.Utilities;

namespace Xirorig.Miner
{
    internal delegate void NetworkConnected(bool daemon, string host, string ip);

    internal delegate void NetworkDisconnected(string host, string reason, Exception? exception);

    internal delegate void NetworkNewJob(IJobTemplate template, string host, string difficulty, string algorithm, string height);

    internal delegate void NetworkJobResult(bool accepted, string difficulty, double ping, string reason);

    internal record MinerNetworkInfo(bool IsDaemon, string Host, string Algorithm);

    internal abstract class MinerNetwork : IDisposable
    {
        public event NetworkConnected? Connected;
        public event NetworkDisconnected? Disconnected;
        public event NetworkNewJob? NewJob;
        public event NetworkJobResult? JobResult;

        public static readonly string DefaultUserAgent = $"{ApplicationUtility.Name}/{ApplicationUtility.Version}";

        protected static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
        {
            Converters = { new BigIntegerJsonConverter(), new JsonStringEnumConverter() },
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals
        };

        private readonly Pool _pool;
        private readonly CancellationToken _globalCancellationToken;

        private MinerNetworkInfo? _minerNetworkInfo;
        private CancellationTokenSource _cancellationTokenSource;

        protected abstract string AlgorithmName { get; }

        protected CancellationToken CancellationToken => _cancellationTokenSource.Token;

        protected MinerNetwork(Pool pool, CancellationToken cancellationToken)
        {
            _pool = pool;
            _globalCancellationToken = cancellationToken;
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_globalCancellationToken);
        }

        public static MinerNetwork CreateNetwork(ProgramContext context, Pool pool)
        {
            var poolAlgorithm = pool.GetAlgorithm();

            if (poolAlgorithm.Equals("xiropht_decentralized", StringComparison.OrdinalIgnoreCase))
            {
                return new XirophtDecentralizedNetwork(pool, context.Options.GetMaxPingTime(), context.ApplicationShutdownCancellationToken);
            }

            throw new NotImplementedException($"{pool.Algorithm} is not implemented.");
        }

        public static MinerNetwork[] CreateDevNetworks(ProgramContext context)
        {
            return new[]
            {
                CreateNetwork(context, new Pool
                {
                    Algorithm = "Xiropht_Decentralized",
                    Coin = "Xiropht",
                    Url = "127.0.0.1:2401",
                    Username = "3tzCoDt5nBykMhEEXgnsiKrDNyqT7BmWUzsU1zf1ivhFCHfbZXJJ9YDbmBeKCG1b6k3419BRZuXGnWECtUxncnGB3JYxjvNARKqdFpDads89RL",
                    UserAgent = DefaultUserAgent,
                    Daemon = true
                })
            };
        }

        public MinerNetworkInfo GetNetworkInfo()
        {
            if (_minerNetworkInfo != null) return _minerNetworkInfo;

            _minerNetworkInfo = new MinerNetworkInfo(_pool.GetIsDaemon(), _pool.GetUrl(), AlgorithmName);
            return _minerNetworkInfo;
        }

        public virtual void StartNetwork()
        {
        }

        public virtual void StopNetwork()
        {
            var initialValue = _cancellationTokenSource;
            var valueToExchange = CancellationTokenSource.CreateLinkedTokenSource(_globalCancellationToken);
            var cancellationTokenSource = Interlocked.CompareExchange(ref _cancellationTokenSource, valueToExchange, initialValue);
            if (cancellationTokenSource == valueToExchange) return;

            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
        }

        public abstract Task SubmitJobAsync(IJobTemplate jobTemplate, IJobResult jobResult);

        protected void OnConnected()
        {
            var hostname = _pool.GetUrl();
            hostname = hostname[hostname.IndexOf(':')..];

            try
            {
                Connected?.Invoke(_pool.GetIsDaemon(), _pool.GetUrl(), Dns.GetHostEntry(hostname).AddressList[0].ToString());
            }
            catch (SocketException)
            {
                Connected?.Invoke(_pool.GetIsDaemon(), _pool.GetUrl(), string.Empty);
            }
        }

        protected void OnDisconnected(string reason, Exception? exception = null)
        {
            Disconnected?.Invoke(_pool.GetUrl(), reason, exception);
        }

        protected void OnNewJob(IJobTemplate jobTemplate, string difficulty, string height)
        {
            NewJob?.Invoke(jobTemplate, _pool.GetUrl(), difficulty, AlgorithmName, height);
        }

        protected void OnJobResult(bool isAccepted, string difficulty, double ping, string? reason = null)
        {
            JobResult?.Invoke(isAccepted, difficulty, ping, reason ?? string.Empty);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}