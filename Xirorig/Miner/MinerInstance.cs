﻿using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheDialgaTeam.Core.Logger.Serilog.Formatting.Ansi;
using Xirorig.Network;
using Xirorig.Network.Api.JobResult;
using Xirorig.Network.Api.JobTemplate;
using Xirorig.Options;
using CpuMiner = Xirorig.Miner.Backend.CpuMiner;

namespace Xirorig.Miner
{
    internal class MinerInstance : IDisposable
    {
        private readonly ProgramContext _context;
        private readonly Options.MinerInstance _minerInstanceOptions;
        private readonly int _minerIndex;

        private readonly MinerNetwork[] _networks;

        private readonly CpuMiner?[] _cpuMiners;

        private readonly Timer _calculateAverageHashCalculatedIn10Seconds;
        private readonly Timer _calculateAverageHashCalculatedIn60Seconds;
        private readonly Timer _calculateAverageHashCalculatedIn15Minutes;

        private MinerNetwork? _currentNetwork;
        private int _currentNetworkIndex;
        private int _retryCount;

        private readonly object _submitLock = new();
        private IJobTemplate? _lastJobTemplateSubmitted;

        public float[] AverageHashCalculatedIn10Seconds { get; }

        public float[] AverageHashCalculatedIn60Seconds { get; }

        public float[] AverageHashCalculatedIn15Minutes { get; }

        public ulong TotalGoodJobsSubmitted { get; private set; }

        public ulong TotalBadJobsSubmitted { get; private set; }

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
                _networks[i] = MinerNetwork.CreateNetwork(context, pools[i]);
                context.Logger.LogInformation($" {AnsiEscapeCodeConstants.GreenForegroundColor}* {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Category,-12:l}} {{Pool:l}}{AnsiEscapeCodeConstants.Reset}", false, $"POOL #{i + 1}", _networks[i].GetNetworkInfo());
            }

            context.Logger.LogInformation($"{AnsiEscapeCodeConstants.DarkGrayForegroundColor}{{Dash:l}}{AnsiEscapeCodeConstants.Reset}", false, "=".PadRight(75, '='));

            var cpuMiner = minerInstanceOptions.GetCpuMiner();
            var numberOfThreads = cpuMiner.GetNumberOfThreads();

            _cpuMiners = new CpuMiner[numberOfThreads];

            _calculateAverageHashCalculatedIn10Seconds = new Timer(CalculateAverageHashCalculatedIn10Seconds, null, Timeout.Infinite, Timeout.Infinite);
            _calculateAverageHashCalculatedIn60Seconds = new Timer(CalculateAverageHashCalculatedIn60Seconds, null, Timeout.Infinite, Timeout.Infinite);
            _calculateAverageHashCalculatedIn15Minutes = new Timer(CalculateAverageHashCalculatedIn15Minutes, null, Timeout.Infinite, Timeout.Infinite);

            AverageHashCalculatedIn10Seconds = new float[numberOfThreads];
            AverageHashCalculatedIn60Seconds = new float[numberOfThreads];
            AverageHashCalculatedIn15Minutes = new float[numberOfThreads];
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

            _calculateAverageHashCalculatedIn10Seconds.Change(0, Timeout.Infinite);
            _calculateAverageHashCalculatedIn60Seconds.Change(0, Timeout.Infinite);
            _calculateAverageHashCalculatedIn15Minutes.Change(0, Timeout.Infinite);
        }

        private void SwapNetwork(MinerNetwork network)
        {
            if (_currentNetwork != null)
            {
                _currentNetwork.StopNetwork();
                UnregisterNetworkEvent(_currentNetwork);
            }

            var cpuMinerConfig = _minerInstanceOptions.GetCpuMiner();
            var cpuMinerThreads = cpuMinerConfig.GetThreads();
            var pool = _minerInstanceOptions.GetPools();

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
            _context.Logger.LogInformation($"{AnsiEscapeCodeConstants.DarkBlueBackgroundColor}MINER #{{Index}}{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.DarkGrayForegroundColor}use pool {AnsiEscapeCodeConstants.CyanForegroundColor}{{Pool:l}}{AnsiEscapeCodeConstants.Reset}", true, _minerIndex + 1, pool.GetUrl());
        }

        private void CurrentNetworkOnDisconnected(Pool pool, Exception? exception)
        {
            if (exception is TaskCanceledException)
            {
                _context.Logger.LogInformation($"{AnsiEscapeCodeConstants.DarkBlueBackgroundColor}MINER #{{Index}}{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.RedForegroundColor}[{{Pool:l}}] Timeout{AnsiEscapeCodeConstants.Reset}", true, _minerIndex + 1, pool.GetUrl());
            }
            else if (exception is HttpRequestException)
            {
                _context.Logger.LogInformation($"{AnsiEscapeCodeConstants.DarkBlueBackgroundColor}MINER #{{Index}}{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.RedForegroundColor}[{{Pool:l}}] Disconnected{AnsiEscapeCodeConstants.Reset}", true, _minerIndex + 1, pool.GetUrl());
            }
            else if (exception != null)
            {
                _context.Logger.LogError(exception, $"{AnsiEscapeCodeConstants.DarkBlueBackgroundColor}MINER #{{Index}}{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.RedForegroundColor}[{{Pool:l}}] Oops, this miner has caught an exception.{AnsiEscapeCodeConstants.Reset}", true, _minerIndex + 1, pool.GetUrl());
                StopInstance();
                return;
            }

            Interlocked.Increment(ref _retryCount);

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
                _context.Logger.LogInformation($"{AnsiEscapeCodeConstants.DarkBlueBackgroundColor}MINER #{{Index}}{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.GreenForegroundColor}accepted{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.DarkGrayForegroundColor}({{Good}}/{{Bad}}) diff{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Difficulty:l}}{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.GrayForegroundColor}({{Ping:F0}} ms){AnsiEscapeCodeConstants.Reset}", true, _minerIndex + 1, TotalGoodJobsSubmitted, TotalBadJobsSubmitted, difficulty, ping);
            }
            else
            {
                TotalBadJobsSubmitted++;
                _context.Logger.LogInformation($"{AnsiEscapeCodeConstants.DarkBlueBackgroundColor}MINER #{{Index}}{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.RedForegroundColor}rejected{AnsiEscapeCodeConstants.Reset} ({{Good}}/{{Bad}}) {{Reason:l}}", true, _minerIndex + 1, TotalGoodJobsSubmitted, TotalBadJobsSubmitted, reason);
            }
        }

        private void CurrentNetworkOnNewJob(Pool pool, IJobTemplate jobTemplate, string difficulty, string algorithm, ulong height)
        {
            _context.Logger.LogInformation($"{AnsiEscapeCodeConstants.DarkBlueBackgroundColor}MINER #{{Index}}{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.MagentaForegroundColor}new job {AnsiEscapeCodeConstants.DarkGrayForegroundColor}from {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Pool:l}} {AnsiEscapeCodeConstants.DarkGrayForegroundColor}diff {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Difficulty:l}} {AnsiEscapeCodeConstants.DarkGrayForegroundColor}algo {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Algorithm:l}} {AnsiEscapeCodeConstants.DarkGrayForegroundColor}height {AnsiEscapeCodeConstants.WhiteForegroundColor}{{Height}}{AnsiEscapeCodeConstants.Reset}", true, _minerIndex + 1, pool.GetUrl(), difficulty, algorithm, height);

            foreach (var cpuMiner in _cpuMiners)
            {
                cpuMiner?.UpdateBlockTemplate(jobTemplate);
            }
        }

        private void CurrentCpuMinerOnJobLog(string message, bool includeDefaultTemplate, object[] args)
        {
            _context.Logger.LogDebug(message, true, args);
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

        private void CalculateAverageHashCalculatedIn10Seconds(object? state)
        {
            var startTime = DateTime.Now;

            for (var i = _cpuMiners.Length - 1; i >= 0; i--)
            {
                var cpuMiner = _cpuMiners[i];
                if (cpuMiner == null) continue;

                AverageHashCalculatedIn10Seconds[i] = cpuMiner.TotalHashCalculatedIn10Seconds / (float) (10 + (DateTime.Now - startTime).TotalSeconds);
                cpuMiner.TotalHashCalculatedIn10Seconds = 0;
            }
        }

        private void CalculateAverageHashCalculatedIn60Seconds(object? state)
        {
            var startTime = DateTime.Now;

            for (var i = _cpuMiners.Length - 1; i >= 0; i--)
            {
                var cpuMiner = _cpuMiners[i];
                if (cpuMiner == null) continue;

                AverageHashCalculatedIn60Seconds[i] = cpuMiner.TotalHashCalculatedIn60Seconds / (float) (60 + (DateTime.Now - startTime).TotalSeconds);
                cpuMiner.TotalHashCalculatedIn60Seconds = 0;
            }
        }

        private void CalculateAverageHashCalculatedIn15Minutes(object? state)
        {
            var startTime = DateTime.Now;

            for (var i = _cpuMiners.Length - 1; i >= 0; i--)
            {
                var cpuMiner = _cpuMiners[i];
                if (cpuMiner == null) continue;

                AverageHashCalculatedIn15Minutes[i] = cpuMiner.TotalHashCalculatedIn15Minutes / (float) (900 + (DateTime.Now - startTime).TotalSeconds);
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