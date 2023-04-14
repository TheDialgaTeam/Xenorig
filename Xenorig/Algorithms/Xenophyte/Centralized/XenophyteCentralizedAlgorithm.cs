using Microsoft.Extensions.Logging;
using Xenolib.Utilities;
using Xenorig.Algorithms.Xenophyte.Centralized.Networking;
using Xenorig.Options;
using CpuMiner = Xenorig.Algorithms.Xenophyte.Centralized.Miner.CpuMiner;

namespace Xenorig.Algorithms.Xenophyte.Centralized;

internal class XenophyteCentralizedAlgorithm : IAlgorithm
{
    private readonly ILogger _logger;
    private readonly XenorigOptions _options;
    private readonly Pool _pool;

    private readonly NetworkPool _network;
    private readonly CpuMiner _cpuMiner;

    private readonly Timer _printAverageHashTimer;

    private double _maxHash;

    private int _totalGoodEasyBlocksSubmitted;
    private int _totalGoodSemiRandomBlocksSubmitted;
    private int _totalGoodRandomBlocksSubmitted;

    private int _totalBadEasyBlocksSubmitted;
    private int _totalBadSemiRandomBlocksSubmitted;
    private int _totalBadRandomBlocksSubmitted;
    
    private int _lastFoundHeight;
    private object _lastFoundHeightLock = new();

    private int TotalGoodBlocksSubmitted => _totalGoodEasyBlocksSubmitted + _totalGoodSemiRandomBlocksSubmitted + _totalGoodRandomBlocksSubmitted;
    private int TotalBadBlocksSubmitted => _totalBadEasyBlocksSubmitted + _totalBadSemiRandomBlocksSubmitted + _totalBadRandomBlocksSubmitted;
    
    public XenophyteCentralizedAlgorithm(ILogger logger, XenorigOptions options, Pool pool)
    {
        _logger = logger;
        _options = options;
        _pool = pool;
        _network = new NetworkPool(logger, options, pool);
        _cpuMiner = new CpuMiner(options, logger, pool, _network);
        _printAverageHashTimer = new Timer(PrintAverageHashTimer, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _cpuMiner.FoundBlock += CpuMinerOnFoundBlock;
        _cpuMiner.StartCpuMiner();

        _network.HasNewBlock += NetworkOnHasNewBlock;
        _network.ConnectionFailed += NetworkOnConnectionFailed;
        await _network.StartAsync(cancellationToken);

        _printAverageHashTimer.Change(TimeSpan.FromSeconds(_options.PrintSpeedDuration), TimeSpan.FromSeconds(_options.PrintSpeedDuration));
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _printAverageHashTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        _cpuMiner.FoundBlock -= CpuMinerOnFoundBlock;
        _cpuMiner.StopCpuMiner();

        _network.HasNewBlock -= NetworkOnHasNewBlock;
        _network.ConnectionFailed -= NetworkOnConnectionFailed;
        await _network.StopAsync(cancellationToken);
    }

    public void PrintHashrate()
    {
        _maxHash = Math.Max(_maxHash, _cpuMiner.AverageHashCalculatedIn10Seconds.Sum());

        Logger.PrintCpuMinerSpeedHeader(_logger);

        var length = _cpuMiner.AverageHashCalculatedIn10Seconds.Length;

        for (var i = 0; i < length; i++)
        {
            Logger.PrintCpuMinerSpeedBreakdown(_logger, i, _cpuMiner.AverageHashCalculatedIn10Seconds.GetRef(i), _cpuMiner.AverageHashCalculatedIn60Seconds.GetRef(i), _cpuMiner.AverageHashCalculatedIn15Minutes.GetRef(i));
        }

        Logger.PrintCpuMinerSpeed(_logger, _cpuMiner.AverageHashCalculatedIn10Seconds.Sum(), _cpuMiner.AverageHashCalculatedIn60Seconds.Sum(), _cpuMiner.AverageHashCalculatedIn15Minutes.Sum(), _maxHash);
    }

    public void PrintStats()
    {
        Logger.PrintXenophyteCentralizedStatsHeader(_logger);
        Logger.PrintXenophyteCentralizedStatsGood(_logger, _totalGoodEasyBlocksSubmitted, _totalGoodSemiRandomBlocksSubmitted, _totalGoodRandomBlocksSubmitted, TotalGoodBlocksSubmitted);
        Logger.PrintXenophyteCentralizedStatsBad(_logger, _totalBadEasyBlocksSubmitted, _totalBadSemiRandomBlocksSubmitted, _totalBadRandomBlocksSubmitted, TotalBadBlocksSubmitted);
    }

    public void PrintCurrentJob()
    {
        var blockHeader = _network.BlockHeader;
        Logger.PrintJob(_logger, "current job", _pool.Url, blockHeader.BlockDifficulty, blockHeader.BlockMethod, blockHeader.BlockHeight);
    }

    private void PrintAverageHashTimer(object? state)
    {
        _maxHash = Math.Max(_maxHash, _cpuMiner.AverageHashCalculatedIn10Seconds.Sum());
        Logger.PrintCpuMinerSpeed(_logger, _cpuMiner.AverageHashCalculatedIn10Seconds.Sum(), _cpuMiner.AverageHashCalculatedIn60Seconds.Sum(), _cpuMiner.AverageHashCalculatedIn15Minutes.Sum(), _maxHash);
    }

    private void CpuMinerOnFoundBlock(int height, string jobType, bool isGoodBlock, string reason, double roundTripTime)
    {
        lock (_lastFoundHeightLock)
        {
            if (height == _lastFoundHeight) return;
            _lastFoundHeight = height;
        
            if (isGoodBlock)
            {
                switch (jobType)
                {
                    case CpuMiner.JobTypeEasy:
                        _totalGoodEasyBlocksSubmitted++;
                        break;

                    case CpuMiner.JobTypeSemiRandom:
                        _totalGoodSemiRandomBlocksSubmitted++;
                        break;

                    case CpuMiner.JobTypeRandom:
                        _totalGoodRandomBlocksSubmitted++;
                        break;
                }

                Logger.PrintBlockAcceptResult(_logger, TotalGoodBlocksSubmitted, TotalBadBlocksSubmitted, roundTripTime);
            }
            else
            {
                switch (jobType)
                {
                    case CpuMiner.JobTypeEasy:
                        _totalBadEasyBlocksSubmitted++;
                        break;

                    case CpuMiner.JobTypeSemiRandom:
                        _totalBadSemiRandomBlocksSubmitted++;
                        break;

                    case CpuMiner.JobTypeRandom:
                        _totalBadRandomBlocksSubmitted++;
                        break;
                }

                Logger.PrintBlockRejectResult(_logger, TotalGoodBlocksSubmitted, TotalBadBlocksSubmitted, reason, roundTripTime);
            }
        }
    }

    private void NetworkOnHasNewBlock()
    {
        var blockHeader = _network.BlockHeader;
        Logger.PrintJob(_logger, "new job", _pool.Url, blockHeader.BlockDifficulty, blockHeader.BlockMethod, blockHeader.BlockHeight);
        _cpuMiner.UpdateJobTemplate();
    }

    private void NetworkOnConnectionFailed()
    {
        _network.StartAsync().Wait();
    }
}