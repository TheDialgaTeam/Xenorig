using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using TheDialgaTeam.Core.Logger.Serilog.Formatting.Ansi;
using Xirorig.Algorithm.Xiropht.Decentralized;
using Xirorig.Network.Api.JobResult;
using Xirorig.Network.Api.JobTemplate;
using Xirorig.Network.Api.JsonConverter;
using Xirorig.Options;
using Xirorig.Utility;

namespace Xirorig.Network
{
    internal delegate void NetworkConnectionStatus(Pool pool, Exception? exception);

    internal delegate void NetworkJob(Pool pool, IJobTemplate jobTemplate, string difficulty, string algorithm, ulong height);

    internal delegate void NetworkJobResult(Pool pool, bool isAccepted, string reason, string difficulty, double ping);

    internal abstract class MinerNetwork : IDisposable
    {
        public event NetworkConnectionStatus? Connected;
        public event NetworkConnectionStatus? Disconnected;
        public event NetworkJob? NewJob;
        public event NetworkJobResult? JobResult;

        public static readonly string DefaultUserAgent = $"{ApplicationUtility.Name}/{ApplicationUtility.Version}";

        protected static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
        {
            Converters = { new BigIntegerJsonConverter(), new JsonStringEnumConverter() },
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals
        };

        private readonly CancellationToken _applicationShutdownCancellationToken;
        private readonly Pool _pool;

        private CancellationTokenSource? _cancellationTokenSource;

        protected abstract string AlgorithmName { get; }

        protected CancellationToken CancellationToken => _cancellationTokenSource?.Token ?? CancellationToken.None;

        protected MinerNetwork(ProgramContext context, Pool pool)
        {
            _pool = pool;
            _applicationShutdownCancellationToken = context.ApplicationShutdownCancellationToken;
        }

        public static MinerNetwork CreateNetwork(ProgramContext context, Pool pool)
        {
            var poolAlgorithm = pool.GetAlgorithm();

            if (poolAlgorithm.Equals("xiropht_decentralized", StringComparison.OrdinalIgnoreCase))
            {
                return new XirophtDecentralizedNetwork(context, pool);
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
                    UserAgent = DefaultUserAgent
                })
            };
        }

        public string GetNetworkInfo()
        {
            return $"{AnsiEscapeCodeConstants.GreenForegroundColor}{_pool.GetUrl()} {AnsiEscapeCodeConstants.DarkGrayForegroundColor}algo {AnsiEscapeCodeConstants.WhiteForegroundColor}{AlgorithmName}";
        }

        public virtual void StartNetwork()
        {
            if (_cancellationTokenSource != null) return;
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_applicationShutdownCancellationToken);
        }

        public virtual void StopNetwork()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        public abstract Task SubmitJobAsync(IJobTemplate jobTemplate, IJobResult jobResult);

        protected void OnConnected()
        {
            Connected?.Invoke(_pool, null);
        }

        protected void OnDisconnected(Exception exception)
        {
            Disconnected?.Invoke(_pool, exception);
        }

        protected void OnNewJob(IJobTemplate jobTemplate, string difficulty, ulong height)
        {
            NewJob?.Invoke(_pool, jobTemplate, difficulty, AlgorithmName, height);
        }

        protected void OnJobResult(bool isAccepted, string reason, string difficulty, double ping)
        {
            JobResult?.Invoke(_pool, isAccepted, reason, difficulty, ping);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}