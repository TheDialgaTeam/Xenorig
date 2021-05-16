using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TheDialgaTeam.Extensions.Logging.LoggingTemplate;
using TheDialgaTeam.Serilog.Formatting.Ansi;
using Xirorig.Algorithm;
using Xirorig.Miner;
using Xirorig.Network;
using Xirorig.Network.Api.Models;
using Xirorig.Options;
using Xirorig.Options.Xirorig;

namespace Xirorig
{
    internal class ProgramHostedService : IHostedService, IDisposable
    {
        private readonly ILoggerTemplate<ProgramHostedService> _logger;
        private readonly XirorigOptions _xirorigOptions;
        private readonly XirorigNetwork _xirorigNetwork;

        private readonly object _cpuSoloMinerSyncRoot = new();

        private readonly IAlgorithm[] _algorithms;

        private readonly Thread _consoleThread;
        private readonly CancellationToken _cancellationToken;

        private readonly decimal[] _averageHashCalculatedIn10Seconds;
        private readonly decimal[] _averageHashCalculatedIn60Seconds;
        private readonly decimal[] _averageHashCalculatedIn15Minutes;
        private decimal _maxHash;

        private readonly Timer _averageHashCalculatedIn10SecondsTimer;
        private readonly Timer _averageHashCalculatedIn60SecondsTimer;
        private readonly Timer _averageHashCalculatedIn15MinutesTimer;
        private readonly Timer _totalAverageHashCalculatedTimer;

        private readonly CpuSoloMiner[] _cpuSoloMiners;
        private BlockTemplate? _currentBlockTemplate;

        private long _totalGoodBlocksSubmitted;
        private long _totalBadBlocksSubmitted;

        private long _lastHeightFound;

        public ProgramHostedService(ILoggerTemplate<ProgramHostedService> logger, IOptions<XirorigOptions> options, IHostApplicationLifetime hostApplicationLifetime, XirorigNetwork xirorigNetwork, IEnumerable<IAlgorithm> algorithms)
        {
            _logger = logger;
            _xirorigOptions = options.Value;
            _xirorigNetwork = xirorigNetwork;

            _algorithms = algorithms.ToArray();

            _consoleThread = new Thread(StartConsoleThread) { Name = "Console Thread", IsBackground = true };
            _cancellationToken = hostApplicationLifetime.ApplicationStopping;

            _averageHashCalculatedIn10Seconds = new decimal[options.Value.NumberOfThreads];
            _averageHashCalculatedIn60Seconds = new decimal[options.Value.NumberOfThreads];
            _averageHashCalculatedIn15Minutes = new decimal[options.Value.NumberOfThreads];

            _averageHashCalculatedIn10SecondsTimer = new Timer(AverageHashCalculatedIn10SecondsCallback, null, Timeout.Infinite, Timeout.Infinite);
            _averageHashCalculatedIn60SecondsTimer = new Timer(AverageHashCalculatedIn60SecondsCallback, null, Timeout.Infinite, Timeout.Infinite);
            _averageHashCalculatedIn15MinutesTimer = new Timer(AverageHashCalculatedIn15MinutesCallback, null, Timeout.Infinite, Timeout.Infinite);
            _totalAverageHashCalculatedTimer = new Timer(TotalAverageHashCalculatedCallback, null, Timeout.Infinite, Timeout.Infinite);

            _cpuSoloMiners = new CpuSoloMiner[options.Value.NumberOfThreads];
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;
            var frameworkVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName ?? string.Empty;

            Console.Title = $"Xirorig v{version} ({frameworkVersion})";

            _logger.LogInformation($" {AnsiEscapeCodeConstants.GreenForegroundColor}*{AnsiEscapeCodeConstants.Reset} {{Category,-12:l}} {AnsiEscapeCodeConstants.CyanForegroundColor}Xirorig/{{Version:l}}{AnsiEscapeCodeConstants.Reset} {{FrameworkVersion:l}}", false, "ABOUT", version, frameworkVersion);
            _logger.LogInformation($" {AnsiEscapeCodeConstants.GreenForegroundColor}*{AnsiEscapeCodeConstants.Reset} {{Category,-12:l}} {AnsiEscapeCodeConstants.CyanForegroundColor}{{numThreads}}{AnsiEscapeCodeConstants.Reset}", false, "THREADS", _xirorigOptions.NumberOfThreads);

            var peerNodes = _xirorigOptions.PeerNodes;

            for (var i = 0; i < peerNodes.Length; i++)
            {
                _logger.LogInformation($" {AnsiEscapeCodeConstants.GreenForegroundColor}*{AnsiEscapeCodeConstants.Reset} SOLO #{{Index,-6}} {AnsiEscapeCodeConstants.CyanForegroundColor}{{Url:l}}{AnsiEscapeCodeConstants.Reset} algo {{Algorithm:l}}", false, i + 1, peerNodes[i].Url, peerNodes[i].Algorithm.ToString());
            }

            _logger.LogInformation($" {AnsiEscapeCodeConstants.GreenForegroundColor}*{AnsiEscapeCodeConstants.Reset} {{Category,-12:l}} {AnsiEscapeCodeConstants.MagentaForegroundColor}h{AnsiEscapeCodeConstants.Reset}ashrate, {AnsiEscapeCodeConstants.MagentaForegroundColor}s{AnsiEscapeCodeConstants.Reset}tats, {AnsiEscapeCodeConstants.MagentaForegroundColor}j{AnsiEscapeCodeConstants.Reset}ob", false, "COMMANDS");
            _logger.LogInformation($"{AnsiEscapeCodeConstants.GreenForegroundColor}READY (CPU){AnsiEscapeCodeConstants.Reset} threads {AnsiEscapeCodeConstants.CyanForegroundColor}{{ThreadCount}}{AnsiEscapeCodeConstants.Reset}", true, _xirorigOptions.NumberOfThreads);

            _xirorigNetwork.Connected += XirorigNetworkOnConnected;
            _xirorigNetwork.Disconnected += XirorigNetworkOnDisconnected;
            _xirorigNetwork.NewJob += XirorigNetworkOnNewJob;
            _xirorigNetwork.BlockResult += XirorigNetworkOnBlockResult;
            _xirorigNetwork.StartNetwork();

            for (var i = 0; i < _xirorigOptions.NumberOfThreads; i++)
            {
                _cpuSoloMiners[i] = new CpuSoloMiner(i, _xirorigOptions.ThreadPriority);
                _cpuSoloMiners[i].Log += CpuSoloMinerOnLog;
                _cpuSoloMiners[i].BlockFound += CpuSoloMinerOnBlockFound;
            }

            _consoleThread.Start();

            _averageHashCalculatedIn10SecondsTimer.Change(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
            _averageHashCalculatedIn60SecondsTimer.Change(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
            _averageHashCalculatedIn15MinutesTimer.Change(TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15));
            _totalAverageHashCalculatedTimer.Change(TimeSpan.FromSeconds(_xirorigOptions.PrintTime), TimeSpan.FromSeconds(_xirorigOptions.PrintTime));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _xirorigNetwork.StopNetwork();
            _xirorigNetwork.Connected -= XirorigNetworkOnConnected;
            _xirorigNetwork.Disconnected -= XirorigNetworkOnDisconnected;
            _xirorigNetwork.NewJob -= XirorigNetworkOnNewJob;
            _xirorigNetwork.BlockResult -= XirorigNetworkOnBlockResult;

            for (var i = 0; i < _xirorigOptions.NumberOfThreads; i++)
            {
                _cpuSoloMiners[i].StopMining();
                _cpuSoloMiners[i].Log -= CpuSoloMinerOnLog;
                _cpuSoloMiners[i].BlockFound -= CpuSoloMinerOnBlockFound;
            }

            _averageHashCalculatedIn10SecondsTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _averageHashCalculatedIn60SecondsTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _averageHashCalculatedIn15MinutesTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _totalAverageHashCalculatedTimer.Change(Timeout.Infinite, Timeout.Infinite);

            return Task.CompletedTask;
        }

        private void StartConsoleThread()
        {
            var threadCount = _xirorigOptions.NumberOfThreads;

            while (!_cancellationToken.IsCancellationRequested)
            {
                var keyPressed = Console.ReadKey(true);

                switch (keyPressed.KeyChar)
                {
                    case 'h':
                        decimal average10SecondsSum = 0, average60SecondsSum = 0, average15MinutesSum = 0;

                        for (var i = 0; i < threadCount; i++)
                        {
                            average10SecondsSum += _averageHashCalculatedIn10Seconds[i];
                            average60SecondsSum += _averageHashCalculatedIn60Seconds[i];
                            average15MinutesSum += _averageHashCalculatedIn15Minutes[i];
                        }

                        _logger.LogInformation("| THREAD | 10s H/s | 60s H/s | 15m H/s |", true);

                        for (var i = 0; i < threadCount; i++)
                        {
                            _logger.LogInformation("| {i,-6} | {j,-7:F1} | {k,-7:F1} | {l,-7:F1} |", true, i, _averageHashCalculatedIn10Seconds[i], _averageHashCalculatedIn60Seconds[i], _averageHashCalculatedIn15Minutes[i]);
                        }

                        _logger.LogInformation($"speed 10s/60s/15m {AnsiEscapeCodeConstants.CyanForegroundColor}{{Average10SecondsSum:F1}}{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.BlueForegroundColor}{{Average60SecondsSum:F1}} {{Average15MinutesSum:F0}}{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.CyanForegroundColor}H/s{AnsiEscapeCodeConstants.Reset} max {AnsiEscapeCodeConstants.CyanForegroundColor}{{MaxHash:F1}}{AnsiEscapeCodeConstants.Reset}", true, average10SecondsSum, average60SecondsSum, average15MinutesSum, _maxHash);
                        break;

                    case 's':
                        _logger.LogInformation($"{AnsiEscapeCodeConstants.GreenForegroundColor}Good:{AnsiEscapeCodeConstants.Reset} {{GoodBlocks}} | {AnsiEscapeCodeConstants.RedForegroundColor}Bad:{AnsiEscapeCodeConstants.Reset} {{BadBlocks}}", true, _totalGoodBlocksSubmitted, _totalBadBlocksSubmitted);
                        break;

                    case 'j':
                        _logger.LogInformation($"{AnsiEscapeCodeConstants.MagentaForegroundColor}current job{AnsiEscapeCodeConstants.Reset} from {{PeerNode:l}} diff {{BlockDifficulty:l}} algo {{Algorithm:l}} height {{Height}}", true, _xirorigNetwork.CurrentPeerNode.Url, _currentBlockTemplate?.CurrentBlockDifficulty.ToString() ?? "0", _xirorigNetwork.CurrentPeerNode.Algorithm.ToString(), _currentBlockTemplate?.CurrentBlockHeight ?? 0);
                        break;
                }
            }
        }

        private void XirorigNetworkOnConnected(PeerNode peerNode)
        {
            _logger.LogInformation($"use solo {AnsiEscapeCodeConstants.CyanForegroundColor}{{PeerNode:l}}{AnsiEscapeCodeConstants.Reset}", true, peerNode.Url);

            foreach (var cpuSoloMiner in _cpuSoloMiners)
            {
                cpuSoloMiner.StartMining(peerNode.WalletAddress, _algorithms.First(algorithm => algorithm.AlgorithmType == peerNode.Algorithm));
            }
        }

        private void XirorigNetworkOnDisconnected(PeerNode peerNode)
        {
            _logger.LogInformation($"{AnsiEscapeCodeConstants.RedForegroundColor}[{{PeerNode:l}}] Disconnected{AnsiEscapeCodeConstants.Reset}", true, peerNode.Url);

            foreach (var cpuSoloMiner in _cpuSoloMiners)
            {
                cpuSoloMiner.StopMining();
            }
        }

        private void XirorigNetworkOnNewJob(PeerNode peerNode, BlockTemplate blockTemplate)
        {
            _currentBlockTemplate = blockTemplate;
            _logger.LogInformation($"{AnsiEscapeCodeConstants.MagentaForegroundColor}new job{AnsiEscapeCodeConstants.Reset} from {{PeerNode:l}} diff {{BlockDifficulty:l}} algo {{Algorithm:l}} height {{Height}}", true, peerNode.Url, blockTemplate.CurrentBlockDifficulty.ToString(), peerNode.Algorithm.ToString(), blockTemplate.CurrentBlockHeight);

            foreach (var cpuSoloMiner in _cpuSoloMiners)
            {
                cpuSoloMiner.UpdateBlockTemplate(blockTemplate);
            }
        }

        private void XirorigNetworkOnBlockResult(bool accepted, string reason)
        {
            if (accepted)
            {
                _totalGoodBlocksSubmitted++;
                _logger.LogInformation($"{AnsiEscapeCodeConstants.GreenForegroundColor}accepted{AnsiEscapeCodeConstants.Reset} ({{Good}}/{{Bad}})", true, _totalGoodBlocksSubmitted, _totalBadBlocksSubmitted);
            }
            else
            {
                _totalBadBlocksSubmitted++;
                _logger.LogInformation($"{AnsiEscapeCodeConstants.RedForegroundColor}rejected{AnsiEscapeCodeConstants.Reset} ({{Good}}/{{Bad}}) - {{Reason:l}}", true, _totalGoodBlocksSubmitted, _totalBadBlocksSubmitted, reason);
            }
        }

        private void CpuSoloMinerOnLog(string message, bool includeDateTime, object[] args)
        {
            _logger.LogInformation(message, includeDateTime, args);
        }

        private async void CpuSoloMinerOnBlockFound(long heightFound, string shareJson)
        {
            //lock (_cpuSoloMinerSyncRoot)
            //{
            //    if (_lastHeightFound == heightFound) return;
            //    _lastHeightFound = heightFound;
            //}

            await _xirorigNetwork.SendMiningShareAsync(shareJson);
        }

        private void AverageHashCalculatedIn10SecondsCallback(object? state)
        {
            var threadLength = _cpuSoloMiners.Length;
            var averageHashCalculatedIn10Seconds = _averageHashCalculatedIn10Seconds;

            for (var i = 0; i < threadLength; i++)
            {
                var cpuSoloMiner = _cpuSoloMiners[i];

                averageHashCalculatedIn10Seconds[i] = cpuSoloMiner.TotalHashCalculatedIn10Seconds / 10m;
                cpuSoloMiner.TotalHashCalculatedIn10Seconds = 0;
            }
        }

        private void AverageHashCalculatedIn60SecondsCallback(object? state)
        {
            var threadLength = _cpuSoloMiners.Length;
            var averageHashCalculatedIn60Seconds = _averageHashCalculatedIn60Seconds;

            for (var i = 0; i < threadLength; i++)
            {
                var cpuSoloMiner = _cpuSoloMiners[i];

                averageHashCalculatedIn60Seconds[i] = cpuSoloMiner.TotalHashCalculatedIn60Seconds / 60m;
                cpuSoloMiner.TotalHashCalculatedIn60Seconds = 0;
            }
        }

        private void AverageHashCalculatedIn15MinutesCallback(object? state)
        {
            var threadLength = _cpuSoloMiners.Length;
            var averageHashCalculatedIn15Minutes = _averageHashCalculatedIn15Minutes;

            for (var i = 0; i < threadLength; i++)
            {
                var cpuSoloMiner = _cpuSoloMiners[i];

                averageHashCalculatedIn15Minutes[i] = cpuSoloMiner.TotalHashCalculatedIn15Minutes / 900m;
                cpuSoloMiner.TotalHashCalculatedIn15Minutes = 0;
            }
        }

        private void TotalAverageHashCalculatedCallback(object? state)
        {
            decimal average10SecondsSum = 0, average60SecondsSum = 0, average15MinutesSum = 0;
            var threadCount = _cpuSoloMiners.Length;

            for (var i = 0; i < threadCount; i++)
            {
                average10SecondsSum += _averageHashCalculatedIn10Seconds[i];
                average60SecondsSum += _averageHashCalculatedIn60Seconds[i];
                average15MinutesSum += _averageHashCalculatedIn15Minutes[i];
            }

            _maxHash = Math.Max(_maxHash, average10SecondsSum);
            _logger.LogInformation($"speed 10s/60s/15m {AnsiEscapeCodeConstants.CyanForegroundColor}{{Average10SecondsSum:F1}}{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.BlueForegroundColor}{{Average60SecondsSum:F1}} {{Average15MinutesSum:F0}}{AnsiEscapeCodeConstants.Reset} {AnsiEscapeCodeConstants.CyanForegroundColor}H/s{AnsiEscapeCodeConstants.Reset} max {AnsiEscapeCodeConstants.CyanForegroundColor}{{MaxHash:F1}}{AnsiEscapeCodeConstants.Reset}", true, average10SecondsSum, average60SecondsSum, average15MinutesSum, _maxHash);
        }

        public void Dispose()
        {
            for (var i = 0; i < _xirorigOptions.NumberOfThreads; i++)
            {
                _cpuSoloMiners[i].Dispose();
            }

            _averageHashCalculatedIn10SecondsTimer.Dispose();
            _averageHashCalculatedIn60SecondsTimer.Dispose();
            _averageHashCalculatedIn15MinutesTimer.Dispose();
            _totalAverageHashCalculatedTimer.Dispose();
        }
    }
}