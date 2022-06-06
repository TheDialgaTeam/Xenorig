using System.Timers;
using Microsoft.Extensions.Logging;
using Xenorig.Options;

namespace Xenorig.Algorithms.Xenophyte.Centralized;

internal partial class XenophyteCentralizedAlgorithm : IAlgorithm, IDisposable
{
    private readonly ProgramContext _context;
    private readonly ILogger _logger;

    private readonly long[] _averageHashCalculatedIn10Seconds;
    private readonly long[] _averageHashCalculatedIn60Seconds;
    private readonly long[] _averageHashCalculatedIn15Minutes;

    private decimal _maxHash;

    private ulong _totalGoodEasyBlocksSubmitted;
    private ulong _totalGoodSemiRandomBlocksSubmitted;
    private ulong _totalGoodRandomBlocksSubmitted;

    private ulong _totalBadEasyBlocksSubmitted;
    private ulong _totalBadSemiRandomBlocksSubmitted;
    private ulong _totalBadRandomBlocksSubmitted;

    private readonly System.Timers.Timer _averageHashCalculatedIn10SecondsTimer;
    private readonly System.Timers.Timer _averageHashCalculatedIn60SecondsTimer;
    private readonly System.Timers.Timer _averageHashCalculatedIn15MinutesTimer;
    private readonly System.Timers.Timer _totalAverageHashCalculatedTimer;

    private ulong TotalGoodBlocksSubmitted => _totalGoodEasyBlocksSubmitted + _totalGoodSemiRandomBlocksSubmitted + _totalGoodRandomBlocksSubmitted;

    private ulong TotalBadBlocksSubmitted => _totalBadEasyBlocksSubmitted + _totalBadSemiRandomBlocksSubmitted + _totalBadRandomBlocksSubmitted;

    public XenophyteCentralizedAlgorithm(ProgramContext context, ILogger logger, Pool[] pools)
    {
        _context = context;
        _logger = logger;

        // Initialize Network
        _pools = pools;

        // Initialize Cpu Miner
        var minerOptions = context.Options.GetCpuMiner();
        var numThreads = minerOptions.GetNumberOfThreads();

        _cpuMiningThreads = new Thread[numThreads];

        for (var i = 0; i < numThreads; i++)
        {
            var threadId = i;
            _cpuMiningThreads[i] = new Thread(() => ExecuteCpuMinerThread(threadId, minerOptions)) { Priority = minerOptions.GetThreadPriority(i), IsBackground = true };
        }

        _averageHashCalculatedIn10Seconds = new long[numThreads];
        _totalHashCalculatedIn10Seconds = new long[numThreads];

        _averageHashCalculatedIn60Seconds = new long[numThreads];
        _totalHashCalculatedIn60Seconds = new long[numThreads];

        _averageHashCalculatedIn15Minutes = new long[numThreads];
        _totalHashCalculatedIn15Minutes = new long[numThreads];

        _averageHashCalculatedIn10SecondsTimer = new System.Timers.Timer { Enabled = false, AutoReset = true, Interval = TimeSpan.FromSeconds(10).TotalMilliseconds };
        _averageHashCalculatedIn60SecondsTimer = new System.Timers.Timer { Enabled = false, AutoReset = true, Interval = TimeSpan.FromSeconds(60).TotalMilliseconds };
        _averageHashCalculatedIn15MinutesTimer = new System.Timers.Timer { Enabled = false, AutoReset = true, Interval = TimeSpan.FromMinutes(15).TotalMilliseconds };
        _totalAverageHashCalculatedTimer = new System.Timers.Timer { Enabled = false, AutoReset = true, Interval = TimeSpan.FromSeconds(context.Options.GetPrintTime()).TotalMilliseconds };

        _averageHashCalculatedIn10SecondsTimer.Elapsed += AverageHashCalculatedIn10SecondsTimerOnElapsed;
        _averageHashCalculatedIn60SecondsTimer.Elapsed += AverageHashCalculatedIn60SecondsTimerOnElapsed;
        _averageHashCalculatedIn15MinutesTimer.Elapsed += AverageHashCalculatedIn15MinutesTimerOnElapsed;
        _totalAverageHashCalculatedTimer.Elapsed += TotalAverageHashCalculatedTimerOnElapsed;

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
        throw new NotImplementedException();
    }

    public void PrintStats()
    {
        throw new NotImplementedException();
    }

    public void PrintCurrentJob()
    {
        Logger.PrintJob(_logger, "current job", $"{_pools[_poolIndex].Url}:{SeedNodePort}", _blockDifficulty, _blockMethod, _blockId);
    }

    private void AverageHashCalculatedIn10SecondsTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        for (var i = _averageHashCalculatedIn10Seconds.Length - 1; i >= 0; i--)
        {
            _averageHashCalculatedIn10Seconds[i] = (long) (Interlocked.Read(ref _totalHashCalculatedIn10Seconds[i]) / 10.0m);
            Interlocked.Exchange(ref _totalHashCalculatedIn10Seconds[i], 0);
        }
    }

    private void AverageHashCalculatedIn60SecondsTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        for (var i = _averageHashCalculatedIn60Seconds.Length - 1; i >= 0; i--)
        {
            _averageHashCalculatedIn60Seconds[i] = (long) (Interlocked.Read(ref _totalHashCalculatedIn60Seconds[i]) / 60.0m);
            Interlocked.Exchange(ref _totalHashCalculatedIn60Seconds[i], 0);
        }
    }

    private void AverageHashCalculatedIn15MinutesTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        for (var i = _averageHashCalculatedIn15Minutes.Length - 1; i >= 0; i--)
        {
            _averageHashCalculatedIn15Minutes[i] = (long) (Interlocked.Read(ref _totalHashCalculatedIn15Minutes[i]) / 900.0m);
            Interlocked.Exchange(ref _totalHashCalculatedIn15Minutes[i], 0);
        }
    }

    private void TotalAverageHashCalculatedTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        decimal average10SecondsSum = 0, average60SecondsSum = 0, average15MinutesSum = 0;

        for (var i = _averageHashCalculatedIn10Seconds.Length - 1; i >= 0; i--)
        {
            average10SecondsSum += _averageHashCalculatedIn10Seconds[i];
            average60SecondsSum += _averageHashCalculatedIn60Seconds[i];
            average15MinutesSum += _averageHashCalculatedIn15Minutes[i];
        }

        _maxHash = Math.Max(_maxHash, average10SecondsSum);
        Logger.PrintCpuMinerSpeed(_logger, average10SecondsSum, average60SecondsSum, average15MinutesSum, _maxHash);
    }

    public void Dispose()
    {
        _averageHashCalculatedIn10SecondsTimer.Dispose();
        _averageHashCalculatedIn60SecondsTimer.Dispose();
        _averageHashCalculatedIn15MinutesTimer.Dispose();
        _totalAverageHashCalculatedTimer.Dispose();
        _blockHeaderLock.Dispose();
    }
}