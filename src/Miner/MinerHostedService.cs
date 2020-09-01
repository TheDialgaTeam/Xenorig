using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using TheDialgaTeam.Xiropht.Xirorig.Config;
using TheDialgaTeam.Xiropht.Xirorig.Network;
using Timer = System.Timers.Timer;

namespace TheDialgaTeam.Xiropht.Xirorig.Miner
{
    public class MinerHostedService : IHostedService, IDisposable
    {
        private readonly XirorigConfiguration _xirorigConfiguration;
        private readonly XirorigToSeedNetwork _xirorigToSeedNetwork;
        private readonly ILogger<MinerHostedService> _logger;

        private readonly RNGCryptoServiceProvider _rngCryptoServiceProvider = new RNGCryptoServiceProvider();
        private readonly CpuSoloMiner[] _cpuSoloMiners;

        private long _totalGoodEasyBlocksSubmitted;
        private long _totalGoodSemiRandomBlocksSubmitted;
        private long _totalGoodRandomBlocksSubmitted;

        private long _totalBadEasyBlocksSubmitted;
        private long _totalBadSemiRandomBlocksSubmitted;
        private long _totalBadRandomBlocksSubmitted;

        private readonly Dictionary<string, string> _blocksFound = new Dictionary<string, string>();

        private readonly long[] _averageHashCalculatedIn10Seconds;
        private readonly long[] _averageHashCalculatedIn60Seconds;
        private readonly long[] _averageHashCalculatedIn15Minutes;

        private decimal _maxHash;

        private Timer? _averageHashCalculatedIn10SecondsTimer;
        private Timer? _averageHashCalculatedIn60SecondsTimer;
        private Timer? _averageHashCalculatedIn15MinutesTimer;
        private Timer? _totalAverageHashCalculatedTimer;

        private long TotalGoodBlocksSubmitted => _totalGoodEasyBlocksSubmitted + _totalGoodSemiRandomBlocksSubmitted + _totalGoodRandomBlocksSubmitted;

        private long TotalBadBlocksSubmitted => _totalBadEasyBlocksSubmitted + _totalBadSemiRandomBlocksSubmitted + _totalBadRandomBlocksSubmitted;

        public MinerHostedService(XirorigConfiguration xirorigConfiguration, XirorigToSeedNetwork xirorigToSeedNetwork, ILogger<MinerHostedService> logger)
        {
            _xirorigConfiguration = xirorigConfiguration;
            _xirorigToSeedNetwork = xirorigToSeedNetwork;
            _logger = logger;

            _cpuSoloMiners = new CpuSoloMiner[xirorigConfiguration.MinerThreadConfigurations.Length];
            _averageHashCalculatedIn10Seconds = new long[xirorigConfiguration.MinerThreadConfigurations.Length];
            _averageHashCalculatedIn60Seconds = new long[xirorigConfiguration.MinerThreadConfigurations.Length];
            _averageHashCalculatedIn15Minutes = new long[xirorigConfiguration.MinerThreadConfigurations.Length];
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _xirorigToSeedNetwork.Disconnected += XirorigToSeedNetworkOnDisconnected;
            _xirorigToSeedNetwork.LoginResult += XirorigToSeedNetworkOnLoginResult;
            _xirorigToSeedNetwork.NewJob += XirorigToSeedNetworkOnNewJob;
            _xirorigToSeedNetwork.BlockResult += XirorigToSeedNetworkOnBlockResult;

            var minerThreadConfigurations = _xirorigConfiguration.MinerThreadConfigurations;

            for (var i = 0; i < minerThreadConfigurations.Length; i++)
            {
                var cpuSoloMiner = new CpuSoloMiner(_rngCryptoServiceProvider, minerThreadConfigurations[i], i);
                cpuSoloMiner.Log += CpuSoloMinerOnLog;
                cpuSoloMiner.BlockFound += CpuSoloMinerOnBlockFound;
                cpuSoloMiner.StartMining();

                _cpuSoloMiners[i] = cpuSoloMiner;
            }

            await _xirorigToSeedNetwork.StartAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("\u001b[30;1m{timestamp:yyyy-MM-dd HH:mm:ss}\u001b[0m \u001b[32;1mREADY (CPU)\u001b[0m threads \u001b[36;1m{threadCount}\u001b[0m", DateTimeOffset.Now, minerThreadConfigurations.Length);

            _averageHashCalculatedIn10SecondsTimer = new Timer { Enabled = true, AutoReset = true, Interval = TimeSpan.FromSeconds(10).TotalMilliseconds };
            _averageHashCalculatedIn60SecondsTimer = new Timer { Enabled = true, AutoReset = true, Interval = TimeSpan.FromSeconds(60).TotalMilliseconds };
            _averageHashCalculatedIn15MinutesTimer = new Timer { Enabled = true, AutoReset = true, Interval = TimeSpan.FromMinutes(15).TotalMilliseconds };
            _totalAverageHashCalculatedTimer = new Timer { Enabled = true, AutoReset = true, Interval = TimeSpan.FromSeconds(_xirorigConfiguration.PrintTime).TotalMilliseconds };

            _averageHashCalculatedIn10SecondsTimer.Elapsed += AverageHashCalculatedIn10SecondsTimerOnElapsed;
            _averageHashCalculatedIn60SecondsTimer.Elapsed += AverageHashCalculatedIn60SecondsTimerOnElapsed;
            _averageHashCalculatedIn15MinutesTimer.Elapsed += AverageHashCalculatedIn15MinutesTimerOnElapsed;
            _totalAverageHashCalculatedTimer.Elapsed += TotalAverageHashCalculatedTimerOnElapsed;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _xirorigToSeedNetwork.StopAsync().ConfigureAwait(false);

            _xirorigToSeedNetwork.Disconnected -= XirorigToSeedNetworkOnDisconnected;
            _xirorigToSeedNetwork.LoginResult -= XirorigToSeedNetworkOnLoginResult;
            _xirorigToSeedNetwork.NewJob -= XirorigToSeedNetworkOnNewJob;
            _xirorigToSeedNetwork.BlockResult -= XirorigToSeedNetworkOnBlockResult;

            foreach (var cpuSoloMiner in _cpuSoloMiners)
            {
                cpuSoloMiner.StopMining();
                cpuSoloMiner.Log -= CpuSoloMinerOnLog;
                cpuSoloMiner.BlockFound -= CpuSoloMinerOnBlockFound;
            }

            _averageHashCalculatedIn10SecondsTimer!.Stop();
            _averageHashCalculatedIn60SecondsTimer!.Stop();
            _averageHashCalculatedIn15MinutesTimer!.Stop();
            _totalAverageHashCalculatedTimer!.Stop();

            _averageHashCalculatedIn10SecondsTimer!.Elapsed -= AverageHashCalculatedIn10SecondsTimerOnElapsed;
            _averageHashCalculatedIn60SecondsTimer!.Elapsed -= AverageHashCalculatedIn60SecondsTimerOnElapsed;
            _averageHashCalculatedIn15MinutesTimer!.Elapsed -= AverageHashCalculatedIn15MinutesTimerOnElapsed;
            _totalAverageHashCalculatedTimer.Elapsed -= TotalAverageHashCalculatedTimerOnElapsed;
        }

        private void XirorigToSeedNetworkOnDisconnected(string arg)
        {
            _logger.LogInformation("\u001b[30;1m{timestamp:yyyy-MM-dd HH:mm:ss}\u001b[0m \u001b[31;1m[{host:l}] Disconnected\u001b[0m", DateTimeOffset.Now, arg);
        }

        private void XirorigToSeedNetworkOnLoginResult(string arg1, bool arg2)
        {
            _logger.LogInformation(arg2 ? "\u001b[30;1m{timestamp:yyyy-MM-dd HH:mm:ss}\u001b[0m use solo \u001b[36;1m{host:l}\u001b[0m" : "\u001b[30;1m{timestamp:yyyy-MM-dd HH:mm:ss}\u001b[0m \u001b[31;1m[{host}] Login failed. Reason: Invalid wallet address.\u001b[0m", DateTimeOffset.Now, arg1);
        }

        private void XirorigToSeedNetworkOnNewJob(string arg1, JObject arg2)
        {
            foreach (var cpuSoloMiner in _cpuSoloMiners)
            {
                cpuSoloMiner.UpdateJob(arg2);
            }

            _logger.LogInformation("\u001b[30;1m{timestamp:yyyy-MM-dd HH:mm:ss}\u001b[0m \u001b[35;1mnew job\u001b[0m from {host:l} diff {blockDifficulty:l} algo {algo:l} height {height:l}", DateTimeOffset.Now, arg1, arg2["DIFFICULTY"]!.Value<string>(), arg2["METHOD"]!.Value<string>(), arg2["ID"]!.Value<string>());
        }

        private void XirorigToSeedNetworkOnBlockResult(bool arg1, string arg2, string arg3)
        {
            if (arg1)
            {
                if (_blocksFound.TryGetValue(arg3, out var jobType))
                {
                    if (jobType.Equals(CpuSoloMiner.JobTypeEasy))
                    {
                        _totalGoodEasyBlocksSubmitted++;
                    }
                    else if (jobType.Equals(CpuSoloMiner.JobTypeSemiRandom))
                    {
                        _totalGoodSemiRandomBlocksSubmitted++;
                    }
                    else
                    {
                        _totalGoodRandomBlocksSubmitted++;
                    }
                }

                _logger.LogInformation("\u001b[30;1m{timestamp:yyyy-MM-dd HH:mm:ss}\u001b[0m \u001b[32;1maccepted\u001b[0m ({good}/{bad})", DateTimeOffset.Now, TotalGoodBlocksSubmitted, TotalBadBlocksSubmitted);
            }
            else
            {
                if (_blocksFound.TryGetValue(arg3, out var jobType))
                {
                    if (jobType.Equals(CpuSoloMiner.JobTypeEasy))
                    {
                        _totalBadEasyBlocksSubmitted++;
                    }
                    else if (jobType.Equals(CpuSoloMiner.JobTypeSemiRandom))
                    {
                        _totalBadSemiRandomBlocksSubmitted++;
                    }
                    else
                    {
                        _totalBadRandomBlocksSubmitted++;
                    }
                }

                _logger.LogInformation("\u001b[30;1m{timestamp:yyyy-MM-dd HH:mm:ss}\u001b[0m \u001b[31;1mrejected\u001b[0m ({good}/{bad}) - {reason:l}", DateTimeOffset.Now, TotalGoodBlocksSubmitted, TotalBadBlocksSubmitted, arg2);
            }
        }

        private void CpuSoloMinerOnLog(string arg1, object[] arg2)
        {
            _logger.LogInformation(arg1, arg2);
        }

        private void CpuSoloMinerOnBlockFound(string arg1, string arg2, string arg3)
        {
            _blocksFound.TryAdd(arg1, arg2);
            _xirorigToSeedNetwork.SendPacketToNetworkAsync(arg3, true).GetAwaiter().GetResult();
        }

        private void AverageHashCalculatedIn10SecondsTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            var threadLength = _cpuSoloMiners.Length;
            var averageHashCalculatedIn10Seconds = _averageHashCalculatedIn10Seconds;

            for (var i = 0; i < threadLength; i++)
            {
                var cpuSoloMiner = _cpuSoloMiners[i];

                averageHashCalculatedIn10Seconds[i] = (long) (cpuSoloMiner.TotalHashCalculatedIn10Seconds / 10.0m);
                cpuSoloMiner.TotalHashCalculatedIn10Seconds = 0;
            }
        }

        private void AverageHashCalculatedIn60SecondsTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            var threadLength = _cpuSoloMiners.Length;
            var averageHashCalculatedIn60Seconds = _averageHashCalculatedIn60Seconds;

            for (var i = 0; i < threadLength; i++)
            {
                var cpuSoloMiner = _cpuSoloMiners[i];

                averageHashCalculatedIn60Seconds[i] = (long) (cpuSoloMiner.TotalHashCalculatedIn60Seconds / 60.0m);
                cpuSoloMiner.TotalHashCalculatedIn60Seconds = 0;
            }
        }

        private void AverageHashCalculatedIn15MinutesTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            var threadLength = _cpuSoloMiners.Length;
            var averageHashCalculatedIn15Minutes = _averageHashCalculatedIn15Minutes;

            for (var i = 0; i < threadLength; i++)
            {
                var cpuSoloMiner = _cpuSoloMiners[i];

                averageHashCalculatedIn15Minutes[i] = (long) (cpuSoloMiner.TotalHashCalculatedIn15Minutes / 900.0m);
                cpuSoloMiner.TotalHashCalculatedIn15Minutes = 0;
            }
        }

        private void TotalAverageHashCalculatedTimerOnElapsed(object sender, ElapsedEventArgs e)
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
            _logger.LogInformation("\u001b[30;1m{timestamp:yyyy-MM-dd HH:mm:ss}\u001b[0m speed 10s/60s/15m \u001b[36;1m{average10SecondsSum:F0}\u001b[0m \u001b[36m{average60SecondsSum:F0} {average15MinutesSum:F0}\u001b[0m \u001b[36;1mH/s\u001b[0m max \u001b[36;1m{maxHash:F0}\u001b[0m", DateTimeOffset.Now, average10SecondsSum, average60SecondsSum, average15MinutesSum, _maxHash);
        }

        public void Dispose()
        {
            _rngCryptoServiceProvider.Dispose();
            _averageHashCalculatedIn10SecondsTimer?.Dispose();
            _averageHashCalculatedIn60SecondsTimer?.Dispose();
            _averageHashCalculatedIn15MinutesTimer?.Dispose();
        }
    }
}