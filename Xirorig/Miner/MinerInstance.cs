using System;
using System.Text.Json;
using System.Threading;
using TheDialgaTeam.Core.Logger.Serilog.Formatting.Ansi;
using Xirorig.Miner.Network.Api.JobResult;
using Xirorig.Miner.Network.Api.JobTemplate;
using Xirorig.Options;
using CpuMiner = Xirorig.Miner.Backend.CpuMiner;

namespace Xirorig.Miner
{
    internal record CpuMinerHashCalculated(long TotalHashCalculated, TimeSpan Duration);

    internal record MinerInstanceCurrentJob(string Host, string Difficulty, string Algorithm, string Height);

    internal class MinerInstance : IDisposable
    {
        private readonly ProgramContext _context;
        private readonly Options.MinerInstance _minerInstanceOptions;
        private readonly int _minerIndex;

        private readonly MinerNetwork[] _networks;
        private readonly MinerNetwork[] _donorNetworks;

        private readonly CpuMiner?[] _cpuMiners;

        private readonly Timer _calculateAverageHashSample;

        private MinerNetwork? _currentNetwork;
        private int _currentNetworkIndex;
        private int _retryCount;

        private bool _isDonorNetwork;

        private readonly object _submitLock = new();
        private IJobTemplate? _lastJobTemplateSubmitted;

        public ulong TotalSecondsCalculated { get; private set; }

        public float[] AverageHashCalculatedIn10Seconds { get; }

        public float[] AverageHashCalculatedIn60Seconds { get; }

        public float[] AverageHashCalculatedIn15Minutes { get; }

        public ulong TotalGoodJobsSubmitted { get; private set; }

        public ulong TotalBadJobsSubmitted { get; private set; }

        public MinerInstanceCurrentJob? CurrentJob { get; private set; }

        public MinerInstance(ProgramContext context, int minerIndex, Options.MinerInstance minerInstanceOptions)
        {
            _context = context;
            _minerInstanceOptions = minerInstanceOptions;
            _minerIndex = minerIndex;

            var pools = minerInstanceOptions.GetPools();
            var poolLength = pools.Length;
            if (poolLength == 0) throw new JsonException($"{minerInstanceOptions.Pools} is empty.");

            context.Logger.LogInformation($"{AnsiEscapeCodeConstants.DarkGrayForegroundColor}{{Dash:l}}{AnsiEscapeCodeConstants.Reset}", false, "=".PadRight(75, '='));
            context.Logger.LogInformation($"   {AnsiEscapeCodeConstants.WhiteForegroundColor}MINER INSTANCE #{{Index}}{AnsiEscapeCodeConstants.Reset}", false, minerIndex + 1);
            context.Logger.LogInformation($"{AnsiEscapeCodeConstants.DarkGrayForegroundColor}{{Dash:l}}{AnsiEscapeCodeConstants.Reset}", false, "=".PadRight(75, '='));

            _networks = new MinerNetwork[poolLength];

            for (var i = 0; i < poolLength; i++)
            {
                var network = MinerNetwork.CreateNetwork(context, pools[i]);
                var (isDaemon, host, algorithm) = network.GetNetworkInfo();

                _networks[i] = network;

                context.Logger.LogInformation($" {AnsiEscapeCodeConstants.GreenForegroundColor}* {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Category,-12:l}} {AnsiEscapeCodeConstants.GreenForegroundColor}{{Host:l}} {AnsiEscapeCodeConstants.DarkGrayForegroundColor}algo {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Algorithm:l}}{AnsiEscapeCodeConstants.Reset}", false, $"{(isDaemon ? "SOLO" : "POOL")} #{i + 1}", host, algorithm);
            }

            context.Logger.LogInformation($"{AnsiEscapeCodeConstants.DarkGrayForegroundColor}{{Dash:l}}{AnsiEscapeCodeConstants.Reset}", false, "=".PadRight(75, '='));

            _donorNetworks = MinerNetwork.CreateDevNetworks(context);

            var cpuMiner = minerInstanceOptions.GetCpuMiner();
            var numberOfThreads = cpuMiner.GetNumberOfThreads();

            _cpuMiners = new CpuMiner[numberOfThreads];

            _calculateAverageHashSample = new Timer(CalculateAverageHashSample, null, Timeout.Infinite, Timeout.Infinite);

            AverageHashCalculatedIn10Seconds = new float[numberOfThreads];
            AverageHashCalculatedIn60Seconds = new float[numberOfThreads];
            AverageHashCalculatedIn15Minutes = new float[numberOfThreads];
        }

        public void StartInstance()
        {
            SwapNetwork();
            _calculateAverageHashSample.Change(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
        }

        public void StopInstance()
        {
            foreach (var cpuMiner in _cpuMiners)
            {
                cpuMiner?.StopMining();
            }

            _currentNetwork?.StopNetwork();

            _calculateAverageHashSample.Change(0, Timeout.Infinite);
        }

        private void CalculateAverageHashSample(object? state)
        {
            var startTime = DateTime.Now;

            TotalSecondsCalculated += 10;

            var cpuMinersHashCalculated = new CpuMinerHashCalculated[_cpuMiners.Length];

            for (var i = _cpuMiners.Length - 1; i >= 0; i--)
            {
                var cpuMiner = _cpuMiners[i];

                cpuMinersHashCalculated[i] = new CpuMinerHashCalculated(cpuMiner?.TotalHashCalculated ?? 0, DateTime.Now - startTime);

                if (cpuMiner != null)
                {
                    cpuMiner.TotalHashCalculated = 0;
                }
            }

            for (var i = cpuMinersHashCalculated.Length - 1; i >= 0; i--)
            {
                var nextSample = cpuMinersHashCalculated[i].TotalHashCalculated / (float) (10 + cpuMinersHashCalculated[i].Duration.TotalSeconds);

                AverageHashCalculatedIn10Seconds[i] = nextSample;

                if (TotalSecondsCalculated >= 60)
                {
                    if (AverageHashCalculatedIn60Seconds[i] == 0)
                    {
                        AverageHashCalculatedIn60Seconds[i] = nextSample;
                    }
                    else
                    {
                        AverageHashCalculatedIn60Seconds[i] = (AverageHashCalculatedIn60Seconds[i] + nextSample) / 2;
                    }
                }

                if (TotalSecondsCalculated >= 900)
                {
                    if (AverageHashCalculatedIn15Minutes[i] == 0)
                    {
                        AverageHashCalculatedIn15Minutes[i] = nextSample;
                    }
                    else
                    {
                        AverageHashCalculatedIn15Minutes[i] = (AverageHashCalculatedIn15Minutes[i] + nextSample) / 2;
                    }
                }
            }
        }

        private void SwapNetwork()
        {
            if (_currentNetwork != null)
            {
                _currentNetwork.StopNetwork();
                UnregisterNetworkEvent(_currentNetwork);
            }

            var cpuMinerConfig = _minerInstanceOptions.GetCpuMiner();
            var cpuMinerThreadConfigurations = cpuMinerConfig.GetThreadConfigs();
            var pool = _minerInstanceOptions.GetPools();

            for (var i = 0; i < _cpuMiners.Length; i++)
            {
                var cpuMiner = _cpuMiners[i];
                var cpuMinerThreadConfiguration = i < cpuMinerThreadConfigurations.Length
                    ? cpuMinerThreadConfigurations[i]
                    : new CpuMinerThreadConfiguration
                    {
                        ThreadPriority = cpuMinerConfig.GetThreadPriority(),
                        ThreadAffinity = 0,
                        ExtraParams = cpuMinerConfig.GetExtraParams()
                    };

                cpuMinerThreadConfiguration.ThreadPriority ??= cpuMinerConfig.GetThreadPriority();
                cpuMinerThreadConfiguration.ExtraParams ??= cpuMinerConfig.GetExtraParams();

                if (cpuMiner == null)
                {
                    var newCpuMiner = CpuMiner.CreateCpuMiner(pool[_currentNetworkIndex], cpuMinerThreadConfiguration, _context.ApplicationShutdownCancellationToken);
                    RegisterCpuMinerEvent(newCpuMiner);
                    newCpuMiner.StartMining();

                    _cpuMiners[i] = newCpuMiner;
                }
                else
                {
                    cpuMiner.StopMining();
                    cpuMiner.Dispose();
                    UnregisterCpuMinerEvent(cpuMiner);

                    var newCpuMiner = CpuMiner.CreateCpuMiner(pool[_currentNetworkIndex], cpuMinerThreadConfiguration, _context.ApplicationShutdownCancellationToken);
                    RegisterCpuMinerEvent(newCpuMiner);
                    newCpuMiner.StartMining();

                    _cpuMiners[i] = newCpuMiner;
                }
            }

            _currentNetwork = _networks[_currentNetworkIndex];

            RegisterNetworkEvent(_currentNetwork);
            _currentNetwork.StartNetwork();
        }

        private void RegisterNetworkEvent(MinerNetwork network)
        {
            network.Connected += CurrentNetworkOnConnected;
            network.Disconnected += CurrentNetworkOnDisconnected;
            network.JobResult += CurrentNetworkOnJobResult;
            network.NewJob += CurrentNetworkOnNewJob;
        }

        private void UnregisterNetworkEvent(MinerNetwork network)
        {
            network.Connected -= CurrentNetworkOnConnected;
            network.Disconnected -= CurrentNetworkOnDisconnected;
            network.JobResult -= CurrentNetworkOnJobResult;
            network.NewJob -= CurrentNetworkOnNewJob;
        }

        private void RegisterCpuMinerEvent(CpuMiner cpuMiner)
        {
            cpuMiner.JobResultFound += CurrentCpuMinerOnJobResultFound;
            cpuMiner.Error += CurrentCpuMinerOnError;
        }

        private void UnregisterCpuMinerEvent(CpuMiner cpuMiner)
        {
            cpuMiner.JobResultFound -= CurrentCpuMinerOnJobResultFound;
            cpuMiner.Error -= CurrentCpuMinerOnError;
        }

        private void CurrentNetworkOnConnected(bool daemon, string host, string ip)
        {
            _context.Logger.LogInformation($"{AnsiEscapeCodeConstants.DarkBlueBackgroundColor}MINER #{{Index}}{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.DarkGrayForegroundColor}use {{Mode:l}} {AnsiEscapeCodeConstants.CyanForegroundColor}{{Host:l}} {AnsiEscapeCodeConstants.GrayForegroundColor}{{IpAddress:l}}{AnsiEscapeCodeConstants.Reset}", true, _minerIndex + 1, daemon ? "SOLO" : "POOL", host, ip);
        }

        private void CurrentNetworkOnDisconnected(string host, string reason, Exception? exception)
        {
            if (exception != null)
            {
                _context.Logger.LogError(exception, $"{AnsiEscapeCodeConstants.DarkBlueBackgroundColor}MINER #{{Index}}{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.RedForegroundColor}[{{Pool:l}}] {{Reason:l}}{AnsiEscapeCodeConstants.Reset}", true, _minerIndex + 1, host, reason);
                StopInstance();
                return;
            }

            _context.Logger.LogInformation($"{AnsiEscapeCodeConstants.DarkBlueBackgroundColor}MINER #{{Index}}{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.RedForegroundColor}[{{Pool:l}}] {{Reason:l}}{AnsiEscapeCodeConstants.Reset}", true, _minerIndex + 1, host, reason);

            Interlocked.Increment(ref _retryCount);

            if (_retryCount < _context.Options.GetMaxRetryCount()) return;

            _retryCount = 0;
            _currentNetworkIndex++;

            switch (_isDonorNetwork)
            {
                case false when _currentNetworkIndex >= _networks.Length:
                case true when _currentNetworkIndex >= _donorNetworks.Length:
                    _currentNetworkIndex = 0;
                    break;
            }

            SwapNetwork();
        }

        private void CurrentNetworkOnNewJob(IJobTemplate template, string host, string difficulty, string algorithm, string height)
        {
            _context.Logger.LogInformation($"{AnsiEscapeCodeConstants.DarkBlueBackgroundColor}MINER #{{Index}}{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.MagentaForegroundColor}new job {AnsiEscapeCodeConstants.DarkGrayForegroundColor}from {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Host:l}} {AnsiEscapeCodeConstants.DarkGrayForegroundColor}diff {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Difficulty:l}} {AnsiEscapeCodeConstants.DarkGrayForegroundColor}algo {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Algorithm:l}} {AnsiEscapeCodeConstants.DarkGrayForegroundColor}height {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Height:l}}{AnsiEscapeCodeConstants.Reset}", true, _minerIndex + 1, host, difficulty, algorithm, height);

            foreach (var cpuMiner in _cpuMiners)
            {
                cpuMiner?.UpdateBlockTemplate(template);
            }

            CurrentJob = new MinerInstanceCurrentJob(host, difficulty, algorithm, height);
        }

        private void CurrentNetworkOnJobResult(bool accepted, string difficulty, double ping, string reason)
        {
            if (accepted)
            {
                TotalGoodJobsSubmitted++;
                _context.Logger.LogInformation($"{AnsiEscapeCodeConstants.DarkBlueBackgroundColor}MINER #{{Index}}{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.GreenForegroundColor}accepted {AnsiEscapeCodeConstants.DarkGrayForegroundColor}({{Good}}/{{Bad}}) diff {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Difficulty:l}} {AnsiEscapeCodeConstants.GrayForegroundColor}({{Ping:F0}} ms){AnsiEscapeCodeConstants.Reset}", true, _minerIndex + 1, TotalGoodJobsSubmitted, TotalBadJobsSubmitted, difficulty, ping);
            }
            else
            {
                TotalBadJobsSubmitted++;
                _context.Logger.LogInformation($"{AnsiEscapeCodeConstants.DarkBlueBackgroundColor}MINER #{{Index}}{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.RedForegroundColor}rejected {AnsiEscapeCodeConstants.DarkGrayForegroundColor}({{Good}}/{{Bad}}) diff {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Difficulty:l}} {AnsiEscapeCodeConstants.DarkRedForegroundColor}{{Reason:l}} {AnsiEscapeCodeConstants.GrayForegroundColor}({{Ping:F0}} ms){AnsiEscapeCodeConstants.Reset}", true, _minerIndex + 1, TotalGoodJobsSubmitted, TotalBadJobsSubmitted, difficulty, reason, ping);
            }
        }

        private async void CurrentCpuMinerOnJobResultFound(IJobTemplate jobTemplate, IJobResult jobResult)
        {
            lock (_submitLock)
            {
                if (_lastJobTemplateSubmitted == jobTemplate) return;
                _lastJobTemplateSubmitted = jobTemplate;
            }

            if (_currentNetwork == null) return;
            await _currentNetwork.SubmitJobAsync(jobTemplate, jobResult);
        }

        private void CurrentCpuMinerOnError(Exception exception)
        {
            _context.Logger.LogError(exception, $"{AnsiEscapeCodeConstants.DarkBlueBackgroundColor}MINER #{{Index}}{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.RedForegroundColor}Oops, this miner has caught an exception.{AnsiEscapeCodeConstants.Reset}", true, _minerIndex + 1);
            StopInstance();
        }

        public void Dispose()
        {
            _calculateAverageHashSample.Dispose();

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