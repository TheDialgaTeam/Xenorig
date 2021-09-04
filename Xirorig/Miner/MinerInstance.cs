using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheDialgaTeam.Core.Logger.Serilog.Formatting.Ansi;
using Xirorig.Network;
using Xirorig.Network.Api.JobResult;
using Xirorig.Network.Api.JobTemplate;
using Xirorig.Options;
using Xirorig.Utility;
using CpuMiner = Xirorig.Miner.Backend.CpuMiner;

namespace Xirorig.Miner
{
    internal class MinerInstance : IDisposable
    {
        private readonly ApplicationContext _context;
        private readonly Options.MinerInstance _minerInstance;

        private readonly INetwork[] _networks;
        private readonly INetwork[] _devNetworks;

        private readonly CpuMiner?[] _cpuMiners;

        private readonly Timer _calculateAverageHashCalculatedIn10Seconds;
        private readonly Timer _calculateAverageHashCalculatedIn60Seconds;
        private readonly Timer _calculateAverageHashCalculatedIn15Minutes;

        private INetwork? _currentNetwork;
        private int _currentNetworkIndex = 0;
        private int _retryCount;

        public long[] AverageHashCalculatedIn10Seconds { get; }
        public long[] AverageHashCalculatedIn60Seconds { get; }
        public long[] AverageHashCalculatedIn15Minutes { get; }

        public long TotalGoodJobsSubmitted { get; private set; }
        public long TotalBadJobsSubmitted { get; private set; }

        public MinerInstance(ApplicationContext context, Options.MinerInstance minerInstance, int index)
        {
            _context = context;
            _minerInstance = minerInstance;

            var pools = minerInstance.GetPools();
            var poolLength = pools.Length;
            if (poolLength == 0) throw new JsonException($"{minerInstance.Pools} is empty.");

            context.Logger.LogInformation("{Dash:l}", false, "=".PadRight(75, '='));
            context.Logger.LogInformation("   {Category:l}{Index}", false, "MINER INSTANCE #", index + 1);
            context.Logger.LogInformation("{Dash:l}", false, "=".PadRight(75, '='));

            _networks = new INetwork[poolLength];

            for (var i = 0; i < poolLength; i++)
            {
                _networks[i] = NetworkUtility.CreateNetwork(context, pools[i]);
                context.Logger.LogInformation($" {AnsiEscapeCodeConstants.GreenForegroundColor}*{AnsiEscapeCodeConstants.Reset} {{Category,-12:l}} {AnsiEscapeCodeConstants.GreenForegroundColor}{{Pool:l}}{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.DarkGrayForegroundColor}algo{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Algo:l}}{AnsiEscapeCodeConstants.Reset}", false, $"POOL #{i + 1}", pools[i].GetUrl(), pools[i].GetAlgorithm());
            }

            context.Logger.LogInformation("{Dash:l}", false, "=".PadRight(75, '='));

            _devNetworks = NetworkUtility.CreateDevNetworks(context);

            var cpuMiner = minerInstance.GetCpuMiner();
            var numberOfThreads = cpuMiner.GetNumberOfThreads();

            _cpuMiners = new CpuMiner[numberOfThreads];

            _calculateAverageHashCalculatedIn10Seconds = new Timer(CalculateAverageHashCalculatedIn10Seconds, null, Timeout.Infinite, Timeout.Infinite);
            _calculateAverageHashCalculatedIn60Seconds = new Timer(CalculateAverageHashCalculatedIn60Seconds, null, Timeout.Infinite, Timeout.Infinite);
            _calculateAverageHashCalculatedIn15Minutes = new Timer(CalculateAverageHashCalculatedIn15Minutes, null, Timeout.Infinite, Timeout.Infinite);
 
            AverageHashCalculatedIn10Seconds = new long[numberOfThreads];
            AverageHashCalculatedIn60Seconds = new long[numberOfThreads];
            AverageHashCalculatedIn15Minutes = new long[numberOfThreads];
        }

        public void StartInstance()
        {
            SwapNetwork(_networks[_currentNetworkIndex]);

            _calculateAverageHashCalculatedIn10Seconds.Change(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
            _calculateAverageHashCalculatedIn60Seconds.Change(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));
            _calculateAverageHashCalculatedIn15Minutes.Change(TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15));
        }

        public void StopInstance()
        {
            foreach (var cpuMiner in _cpuMiners)
            {
                cpuMiner?.StopMining();
            }

            _currentNetwork?.StopNetwork();

            _calculateAverageHashCalculatedIn10Seconds.Change(Timeout.Infinite, Timeout.Infinite);
            _calculateAverageHashCalculatedIn60Seconds.Change(Timeout.Infinite, Timeout.Infinite);
            _calculateAverageHashCalculatedIn15Minutes.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void SwapNetwork(INetwork network)
        {
            if (_currentNetwork != null)
            {
                _currentNetwork.StopNetwork();
                UnregisterNetworkEvent(_currentNetwork);
            }

            var cpuMinerConfig = _minerInstance.GetCpuMiner();
            var cpuMinerThreads = cpuMinerConfig.GetThreads();
            var pool = _minerInstance.GetPools();

            for (var i = 0; i < _cpuMiners.Length; i++)
            {
                var cpuMiner = _cpuMiners[i];

                if (cpuMiner == null)
                {
                    var newCpuMiner = CpuMiner.CreateCpuMiner(i + 1, cpuMinerThreads[i], _context.ApplicationShutdownCancellationToken, pool[_currentNetworkIndex]);
                    RegisterCpuMinerEvent(newCpuMiner);
                    newCpuMiner.StartMining();

                    _cpuMiners[i] = newCpuMiner;
                }
                else
                {
                    cpuMiner.StopMining();
                    cpuMiner.Dispose();
                    UnregisterCpuMinerEvent(cpuMiner);

                    var newCpuMiner = CpuMiner.CreateCpuMiner(i + 1, cpuMinerThreads[i], _context.ApplicationShutdownCancellationToken, pool[_currentNetworkIndex]);
                    RegisterCpuMinerEvent(newCpuMiner);
                    newCpuMiner.StartMining();

                    _cpuMiners[i] = newCpuMiner;
                }
            }

            _currentNetwork = network;

            RegisterNetworkEvent(_currentNetwork);
            _currentNetwork.StartNetwork();
        }

        private void RegisterNetworkEvent(INetwork network)
        {
            network.Connected += CurrentNetworkOnConnected;
            network.Disconnected += CurrentNetworkOnDisconnected;
            network.JobResult += CurrentNetworkOnJobResult;
            network.NewJob += CurrentNetworkOnNewJob;
        }

        private void UnregisterNetworkEvent(INetwork network)
        {
            network.Connected -= CurrentNetworkOnConnected;
            network.Disconnected -= CurrentNetworkOnDisconnected;
            network.JobResult -= CurrentNetworkOnJobResult;
            network.NewJob -= CurrentNetworkOnNewJob;
        }

        private void RegisterCpuMinerEvent(CpuMiner cpuMiner)
        {
            cpuMiner.JobLog += CurrentCpuMinerOnJobLog;
            cpuMiner.JobResultFound += CurrentCpuMinerOnJobResultFound;
        }

        private void UnregisterCpuMinerEvent(CpuMiner cpuMiner)
        {
            cpuMiner.JobLog -= CurrentCpuMinerOnJobLog;
            cpuMiner.JobResultFound -= CurrentCpuMinerOnJobResultFound;
        }

        private void CurrentNetworkOnConnected(Pool pool, Exception? exception)
        {
            _context.Logger.LogInformation($"{AnsiEscapeCodeConstants.DarkGrayForegroundColor}use pool{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.CyanForegroundColor}{{Pool:l}}{AnsiEscapeCodeConstants.Reset}", true, pool.GetUrl());
        }

        private void CurrentNetworkOnDisconnected(Pool pool, Exception? exception)
        {
            if (exception is TaskCanceledException)
            {
                _context.Logger.LogInformation($"{AnsiEscapeCodeConstants.RedForegroundColor}[{{Pool:l}}] Timeout{AnsiEscapeCodeConstants.Reset}", true, pool.GetUrl());
            }
            else if (exception is HttpRequestException)
            {
                _context.Logger.LogInformation($"{AnsiEscapeCodeConstants.RedForegroundColor}[{{Pool:l}}] Disconnected{AnsiEscapeCodeConstants.Reset}", true, pool.GetUrl());
            }

            _retryCount++;

            if (_retryCount >= _context.Options.GetMaxRetryCount())
            {
                _retryCount = 0;
                _currentNetworkIndex++;

                if (_currentNetworkIndex >= _networks.Length)
                {
                    _currentNetworkIndex = 0;
                }

                SwapNetwork(_networks[_currentNetworkIndex]);
            }
        }

        private void CurrentNetworkOnJobResult(Pool pool, bool isAccepted, string reason, string difficulty, double ping)
        {
            if (isAccepted)
            {
                TotalGoodJobsSubmitted++;
                _context.Logger.LogInformation($"{AnsiEscapeCodeConstants.GreenForegroundColor}accepted{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.DarkGrayForegroundColor}({{Good}}/{{Bad}}) diff{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Difficulty:l}}{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.GrayForegroundColor}({{Ping:F0}} ms){AnsiEscapeCodeConstants.Reset}", true, TotalGoodJobsSubmitted, TotalBadJobsSubmitted, difficulty, ping);
            }
            else
            {
                TotalBadJobsSubmitted++;
                _context.Logger.LogInformation($"{AnsiEscapeCodeConstants.RedForegroundColor}rejected{AnsiEscapeCodeConstants.Reset} ({{Good}}/{{Bad}}) {{Reason:l}}", true, TotalGoodJobsSubmitted, TotalBadJobsSubmitted, reason);
            }
        }

        private void CurrentNetworkOnNewJob(Pool pool, IJobTemplate jobTemplate, string difficulty, long height)
        {
            _context.Logger.LogInformation($"{AnsiEscapeCodeConstants.MagentaForegroundColor}new job{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.DarkGrayForegroundColor}from{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Pool:l}}{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.DarkGrayForegroundColor}diff{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Difficulty:l}}{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.DarkGrayForegroundColor}algo{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Algorithm:l}}{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.DarkGrayForegroundColor}height{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Height}}{AnsiEscapeCodeConstants.Reset}", true, pool.GetUrl(), difficulty, pool.GetAlgorithm(), height);

            foreach (var cpuMiner in _cpuMiners)
            {
                cpuMiner?.UpdateBlockTemplate(jobTemplate);
            }
        }

        private void CurrentCpuMinerOnJobLog(string message, bool includeDefaultTemplate, object[] args)
        {
            _context.Logger.LogInformation(message, true, args);
        }

        private async void CurrentCpuMinerOnJobResultFound(IJobTemplate jobTemplate, IJobResult jobResult)
        {
            if (_currentNetwork == null) return;
            await _currentNetwork.SubmitJobAsync(jobTemplate, jobResult);
        }

        private void CalculateAverageHashCalculatedIn10Seconds(object? state)
        {
            for (var i = 0; i < _cpuMiners.Length; i++)
            {
                var cpuMiner = _cpuMiners[i];
                if (cpuMiner == null) continue;

                AverageHashCalculatedIn10Seconds[i] = cpuMiner.TotalHashCalculatedIn10Seconds / 10;
                cpuMiner.TotalHashCalculatedIn10Seconds = 0;
            }
        }

        private void CalculateAverageHashCalculatedIn60Seconds(object? state)
        {
            for (var i = 0; i < _cpuMiners.Length; i++)
            {
                var cpuMiner = _cpuMiners[i];
                if (cpuMiner == null) continue;

                AverageHashCalculatedIn60Seconds[i] = cpuMiner.TotalHashCalculatedIn60Seconds / 60;
                cpuMiner.TotalHashCalculatedIn60Seconds = 0;
            }
        }

        private void CalculateAverageHashCalculatedIn15Minutes(object? state)
        {
            for (var i = 0; i < _cpuMiners.Length; i++)
            {
                var cpuMiner = _cpuMiners[i];
                if (cpuMiner == null) continue;

                AverageHashCalculatedIn15Minutes[i] = cpuMiner.TotalHashCalculatedIn15Minutes / 900;
                cpuMiner.TotalHashCalculatedIn15Minutes = 0;
            }
        }

        public void Dispose()
        {
            _calculateAverageHashCalculatedIn10Seconds.Dispose();
            _calculateAverageHashCalculatedIn60Seconds.Dispose();
            _calculateAverageHashCalculatedIn15Minutes.Dispose();

            foreach (var network in _networks)
            {
                network.Dispose();
            }

            foreach (var cpuMiner in _cpuMiners)
            {
                cpuMiner?.Dispose();
            }
        }
    }
}