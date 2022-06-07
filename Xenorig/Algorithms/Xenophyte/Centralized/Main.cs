using System.Timers;
using Microsoft.Extensions.Logging;
using Xenorig.Options;
using Timer = System.Timers.Timer;

namespace Xenorig.Algorithms.Xenophyte.Centralized;

internal partial class XenophyteCentralizedAlgorithm : IAlgorithm, IDisposable
{
    private readonly XenorigOptions _options;
    private readonly ILogger _logger;

    private readonly object _lock = new();

    private readonly double[] _averageHashCalculatedIn10Seconds;
    private readonly double[] _averageHashCalculatedIn60Seconds;
    private readonly double[] _averageHashCalculatedIn15Minutes;

    private int _amountSampledFor60Seconds;
    private int _amountSampledFor15Minutes;

    private double _maxHash;

    private ulong _totalGoodEasyBlocksSubmitted;
    private ulong _totalGoodSemiRandomBlocksSubmitted;
    private ulong _totalGoodRandomBlocksSubmitted;

    private ulong _totalBadEasyBlocksSubmitted;
    private ulong _totalBadSemiRandomBlocksSubmitted;
    private ulong _totalBadRandomBlocksSubmitted;

    private readonly Timer _calculateAverageHashTimer;
    private readonly Timer _printAverageHashTimer;

    private ulong TotalGoodBlocksSubmitted => _totalGoodEasyBlocksSubmitted + _totalGoodSemiRandomBlocksSubmitted + _totalGoodRandomBlocksSubmitted;
    private ulong TotalBadBlocksSubmitted => _totalBadEasyBlocksSubmitted + _totalBadSemiRandomBlocksSubmitted + _totalBadRandomBlocksSubmitted;

    public XenophyteCentralizedAlgorithm(XenorigOptions options, ILogger logger, Pool[] pools)
    {
        _options = options;
        _logger = logger;

        // Initialize Network
        _pools = pools;

        // Initialize Cpu Miner
        var minerOptions = options.GetCpuMiner();
        var numThreads = minerOptions.GetNumberOfThreads();

        _cpuMiningThreads = new Thread[numThreads];

        for (var i = 0; i < numThreads; i++)
        {
            var threadId = i;
            _cpuMiningThreads[i] = new Thread(() => ExecuteCpuMinerThread(threadId, minerOptions)) { Priority = minerOptions.GetThreadPriority(i), IsBackground = true };
        }

        _averageHashCalculatedIn10Seconds = new double[numThreads];
        _totalHashCalculatedIn10Seconds = new long[numThreads];

        _averageHashCalculatedIn60Seconds = new double[numThreads];
        _totalHashCalculatedIn60Seconds = new long[numThreads];

        _averageHashCalculatedIn15Minutes = new double[numThreads];
        _totalHashCalculatedIn15Minutes = new long[numThreads];

        _calculateAverageHashTimer = new Timer { Enabled = false, AutoReset = true, Interval = TimeSpan.FromSeconds(10).TotalMilliseconds };
        _calculateAverageHashTimer.Elapsed += CalculateAverageHashTimerOnElapsed;

        _printAverageHashTimer = new Timer { Enabled = false, AutoReset = true, Interval = TimeSpan.FromSeconds(options.GetPrintTime()).TotalMilliseconds };
        _printAverageHashTimer.Elapsed += PrintAverageHashTimerOnElapsed;

        Logger.PrintCpuMinerReady(_logger, numThreads);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await StartNetworkAsync(cancellationToken);
        StartCpuMiner();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await StopNetworkAsync(cancellationToken);
        StopCpuMiner();
    }

    public void PrintHashrate()
    {
        lock (_lock)
        {
            double average10SecondsSum = 0, average60SecondsSum = 0, average15MinutesSum = 0;

            for (var i = _averageHashCalculatedIn10Seconds.Length - 1; i >= 0; i--)
            {
                average10SecondsSum += _averageHashCalculatedIn10Seconds[i];
                average60SecondsSum += _averageHashCalculatedIn60Seconds[i];
                average15MinutesSum += _averageHashCalculatedIn15Minutes[i];
            }

            Logger.PrintCpuMinerSpeedHeader(_logger);

            for (var i = 0; i < _averageHashCalculatedIn10Seconds.Length; i++)
            {
                Logger.PrintCpuMinerSpeedBreakdown(_logger, i, _averageHashCalculatedIn10Seconds[i], _averageHashCalculatedIn60Seconds[i], _averageHashCalculatedIn15Minutes[i]);
            }

            Logger.PrintCpuMinerSpeed(_logger, average10SecondsSum, average60SecondsSum, average15MinutesSum, _maxHash);
        }
    }

    public void PrintStats()
    {
        Logger.PrintXenophyteCentralizedStatsHeader(_logger);
        Logger.PrintXenophyteCentralizedStatsGood(_logger, _totalGoodEasyBlocksSubmitted, _totalGoodSemiRandomBlocksSubmitted, _totalGoodRandomBlocksSubmitted, TotalGoodBlocksSubmitted);
        Logger.PrintXenophyteCentralizedStatsBad(_logger, _totalBadEasyBlocksSubmitted, _totalBadSemiRandomBlocksSubmitted, _totalBadRandomBlocksSubmitted, TotalBadBlocksSubmitted);
    }

    public void PrintCurrentJob()
    {
        Logger.PrintJob(_logger, "current job", $"{_pools[_poolIndex].Url}:{SeedNodePort}", _blockDifficulty, _blockMethod, _blockId);
    }

    private void CalculateAverageHashTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        lock (_lock)
        {
            if (!_printAverageHashTimer.Enabled) _printAverageHashTimer.Start();

            for (var i = _averageHashCalculatedIn10Seconds.Length - 1; i >= 0; i--)
            {
                _averageHashCalculatedIn10Seconds[i] = Interlocked.Read(ref _totalHashCalculatedIn10Seconds[i]) / (TimeSpan.FromSeconds(10) + (DateTime.Now - e.SignalTime)).TotalSeconds;
                Interlocked.Exchange(ref _totalHashCalculatedIn10Seconds[i], 0);
            }

            if (Interlocked.Add(ref _amountSampledFor60Seconds, 10) >= 60)
            {
                for (var i = _averageHashCalculatedIn60Seconds.Length - 1; i >= 0; i--)
                {
                    _averageHashCalculatedIn60Seconds[i] = Interlocked.Read(ref _totalHashCalculatedIn60Seconds[i]) / (TimeSpan.FromMinutes(1) + (DateTime.Now - e.SignalTime)).TotalSeconds;
                    Interlocked.Exchange(ref _totalHashCalculatedIn60Seconds[i], 0);
                }

                Interlocked.Exchange(ref _amountSampledFor60Seconds, 0);
            }

            if (Interlocked.Add(ref _amountSampledFor15Minutes, 10) >= 60 * 15)
            {
                for (var i = _averageHashCalculatedIn60Seconds.Length - 1; i >= 0; i--)
                {
                    _averageHashCalculatedIn15Minutes[i] = Interlocked.Read(ref _totalHashCalculatedIn15Minutes[i]) / (TimeSpan.FromMinutes(15) + (DateTime.Now - e.SignalTime)).TotalSeconds;
                    Interlocked.Exchange(ref _averageHashCalculatedIn15Minutes[i], 0);
                }

                Interlocked.Exchange(ref _amountSampledFor15Minutes, 0);
            }
        }
    }

    private void PrintAverageHashTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        lock (_lock)
        {
            if (!_calculateAverageHashTimer.Enabled)
            {
                _printAverageHashTimer.Stop();
                return;
            }

            double average10SecondsSum = 0, average60SecondsSum = 0, average15MinutesSum = 0;

            for (var i = _averageHashCalculatedIn10Seconds.Length - 1; i >= 0; i--)
            {
                average10SecondsSum += _averageHashCalculatedIn10Seconds[i];
                average60SecondsSum += _averageHashCalculatedIn60Seconds[i];
                average15MinutesSum += _averageHashCalculatedIn15Minutes[i];
            }

            _maxHash = Math.Max(_maxHash, Math.Max(average10SecondsSum, Math.Max(average60SecondsSum, average15MinutesSum)));
            Logger.PrintCpuMinerSpeed(_logger, average10SecondsSum, average60SecondsSum, average15MinutesSum, _maxHash);
        }
    }

    public void Dispose()
    {
        _calculateAverageHashTimer.Dispose();
        _blockHeaderLock.Dispose();
    }
}