using Microsoft.Extensions.Logging;
using Xenolib.Algorithms.Xenophyte.Centralized.Networking.Solo;
using Xenolib.Utilities;
using Xenorig.Options;
using CpuMiner = Xenorig.Algorithms.Xenophyte.Centralized.Solo.Miner.CpuMiner;

namespace Xenorig.Algorithms.Xenophyte.Centralized.Solo;

public class XenophyteCentralizedSoloAlgorithm : IAlgorithm
{
    private readonly ILogger _logger;
    private readonly XenorigOptions _options;
    private readonly Options.Pool _pool;
    
    private readonly Network _network;
    private readonly NetworkConnection _networkConnection;
    private BlockHeader _blockHeader = new();
    
    private readonly CpuMiner _cpuMiner;

    private readonly Timer _printAverageHashTimer;

    private double _maxHash;

    private int _totalGoodEasyBlocksSubmitted;
    private int _totalGoodSemiRandomBlocksSubmitted;
    private int _totalGoodRandomBlocksSubmitted;

    private int _totalBadEasyBlocksSubmitted;
    private int _totalBadSemiRandomBlocksSubmitted;
    private int _totalBadRandomBlocksSubmitted;
    
    private long _lastFoundHeight;
    private readonly object _lastFoundHeightLock = new();

    private int TotalGoodBlocksSubmitted => _totalGoodEasyBlocksSubmitted + _totalGoodSemiRandomBlocksSubmitted + _totalGoodRandomBlocksSubmitted;
    private int TotalBadBlocksSubmitted => _totalBadEasyBlocksSubmitted + _totalBadSemiRandomBlocksSubmitted + _totalBadRandomBlocksSubmitted;
    
    public XenophyteCentralizedSoloAlgorithm(ILogger logger, XenorigOptions options, Options.Pool pool)
    {
        _logger = logger;
        _options = options;
        _pool = pool;
        _network = new Network();
        _networkConnection = new NetworkConnection { Uri = new UriBuilder(pool.Url) { Port = NetworkConstants.SeedNodePort }.Uri, WalletAddress = pool.Username, TimeoutDuration = TimeSpan.FromSeconds(_options.NetworkTimeoutDuration) };
        _cpuMiner = new CpuMiner(options, logger, pool, _network);
        _printAverageHashTimer = new Timer(PrintAverageHashTimer, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _cpuMiner.FoundBlock += CpuMinerOnFoundBlock;
        _cpuMiner.StartCpuMiner();

        _network.Disconnected += NetworkOnDisconnected;
        _network.Ready += NetworkOnReady;
        _network.HasNewBlock += NetworkOnHasNewBlock;
        await _network.ConnectAsync(_networkConnection, cancellationToken);

        _printAverageHashTimer.Change(TimeSpan.FromSeconds(_options.PrintSpeedDuration), TimeSpan.FromSeconds(_options.PrintSpeedDuration));
    }
    
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _printAverageHashTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        _cpuMiner.FoundBlock -= CpuMinerOnFoundBlock;
        _cpuMiner.StopCpuMiner();

        _network.Disconnected -= NetworkOnDisconnected;
        _network.Ready -= NetworkOnReady;
        _network.HasNewBlock -= NetworkOnHasNewBlock;
        await _network.DisconnectAsync(cancellationToken);
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
        Logger.PrintJob(_logger, "current job", _pool.Url, _blockHeader.BlockDifficulty, _blockHeader.BlockMethod, _blockHeader.BlockHeight);
    }

    private void PrintAverageHashTimer(object? state)
    {
        _maxHash = Math.Max(_maxHash, _cpuMiner.AverageHashCalculatedIn10Seconds.Sum());
        Logger.PrintCpuMinerSpeed(_logger, _cpuMiner.AverageHashCalculatedIn10Seconds.Sum(), _cpuMiner.AverageHashCalculatedIn60Seconds.Sum(), _cpuMiner.AverageHashCalculatedIn15Minutes.Sum(), _maxHash);
    }

    private void CpuMinerOnFoundBlock(long height, string jobType, bool isGoodBlock, string reason, double roundTripTime)
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

    private async void NetworkOnDisconnected(string reason)
    {
        Logger.PrintDisconnected(_logger, _networkConnection.Uri.Host, reason == string.Empty ? "None" : reason);
        await _network.ConnectAsync(_networkConnection);
    }
    
    private void NetworkOnReady()
    {
        Logger.PrintConnected(_logger, "SOLO", _pool.Url);
    }
    
    private void NetworkOnHasNewBlock(BlockHeader blockHeader)
    {
        Logger.PrintJob(_logger, "new job", _pool.Url, blockHeader.BlockDifficulty, blockHeader.BlockMethod, blockHeader.BlockHeight);
        _blockHeader = blockHeader;
        _cpuMiner.UpdateJobTemplate(blockHeader);
    }
}