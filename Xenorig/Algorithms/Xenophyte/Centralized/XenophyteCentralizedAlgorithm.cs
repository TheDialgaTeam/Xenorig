using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xenorig.Algorithms.Xenophyte.Centralized.Networking;
using Xenorig.Options;

namespace Xenorig.Algorithms.Xenophyte.Centralized;

internal class XenophyteCentralizedAlgorithm : IAlgorithm
{
    private readonly ILogger _logger;
    private readonly XenorigOptions _options;

    private readonly Network _network;

    private readonly object _calculateHashrateLock = new();

    private readonly double[] _averageHashCalculatedIn10Seconds;
    private readonly double[] _averageHashCalculatedIn60Seconds;
    private readonly double[] _averageHashCalculatedIn15Minutes;

    private int _amountSampledFor60Seconds;
    private int _amountSampledFor15Minutes;

    private double _maxHash;
    
    private int _totalGoodEasyBlocksSubmitted;
    private int _totalGoodSemiRandomBlocksSubmitted;
    private int _totalGoodRandomBlocksSubmitted;

    private int _totalBadEasyBlocksSubmitted;
    private int _totalBadSemiRandomBlocksSubmitted;
    private int _totalBadRandomBlocksSubmitted;

    private int TotalGoodBlocksSubmitted => _totalGoodEasyBlocksSubmitted + _totalGoodSemiRandomBlocksSubmitted + _totalGoodRandomBlocksSubmitted;
    private int TotalBadBlocksSubmitted => _totalBadEasyBlocksSubmitted + _totalBadSemiRandomBlocksSubmitted + _totalBadRandomBlocksSubmitted;

    public XenophyteCentralizedAlgorithm(ILogger logger, XenorigOptions options, Pool pool)
    {
        _logger = logger;
        _options = options;
        _network = new Network(logger, options, pool);
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return _network.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _network.StopAsync(cancellationToken);
        
        return Task.CompletedTask;
    }

    public void PrintHashrate()
    {
        lock (_calculateHashrateLock)
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
        throw new NotImplementedException();
    }
}