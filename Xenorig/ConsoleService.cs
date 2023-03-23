using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xenorig.Algorithms;
using Xenorig.Algorithms.Xenophyte.Centralized;
using Xenorig.Options;
using Xenorig.Utilities;

namespace Xenorig;

public sealed class ConsoleService : BackgroundService
{
    private readonly ILogger<ConsoleService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly XenorigOptions _options;

    private IAlgorithm[] _minerInstances = Array.Empty<IAlgorithm>();
    private int _currentIndex = 0;

    public ConsoleService(ILogger<ConsoleService> logger, ILoggerFactory loggerFactory, IOptions<XenorigOptions> options)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.Title = $"{ApplicationUtility.Name} v{ApplicationUtility.Version} ({ApplicationUtility.FrameworkVersion})";

        Logger.PrintAbout(_logger, "About", ApplicationUtility.Name, ApplicationUtility.Version, ApplicationUtility.FrameworkVersion);
        Logger.PrintCpu(_logger, "CPU", CpuInformationUtility.ProcessorName, CpuInformationUtility.ProcessorInstructionSetsSupported);
        Logger.PrintCpuCont(_logger, string.Empty, CpuInformationUtility.ProcessorL2Cache / 1024.0 / 1024.0, CpuInformationUtility.ProcessorL3Cache / 1024.0 / 1024.0, CpuInformationUtility.ProcessorCoreCount, CpuInformationUtility.ProcessorThreadCount);
        Logger.PrintDonatePercentage(_logger, "DONATE", _options.DonatePercentage);
        Logger.PrintCommand(_logger, "COMMANDS");

        _minerInstances = CreateMinerInstances();
        if (_minerInstances.Length == 0) return;

        foreach (var minerInstance in _minerInstances)
        {
            await minerInstance.StartAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var readKeyTask = Task.Factory.StartNew(() => Console.ReadKey(true), stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
            var taskCompleted = await Task.WhenAny(readKeyTask, Task.Delay(Timeout.Infinite, stoppingToken)).ConfigureAwait(false);
            if (taskCompleted != readKeyTask) return;

            var keyPressed = await readKeyTask.ConfigureAwait(false);

            switch (keyPressed.Key)
            {
                case ConsoleKey.H:
                    _minerInstances[0].PrintHashrate();
                    break;

                case ConsoleKey.S:
                    _minerInstances[0].PrintStats();
                    break;

                case ConsoleKey.J:
                    _minerInstances[0].PrintCurrentJob();
                    break;
            }
        }
    }

    private IAlgorithm[] CreateMinerInstances()
    {
        var pools = _options.Pools;
        var uniquePool = new List<Pool>();

        var lastAlgorithm = string.Empty;
        var lastCoin = string.Empty;

        var minerInstances = new List<IAlgorithm>();

        foreach (var pool in pools)
        {
            if (uniquePool.Count == 0 || (lastAlgorithm.Equals(pool.Algorithm.Trim(), StringComparison.OrdinalIgnoreCase) && lastCoin.Equals(pool.Coin.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                uniquePool.Add(pool);
            }
            else
            {
                minerInstances.Add(CreateAlgorithm(uniquePool.ToArray()));
                uniquePool.Clear();
            }

            lastAlgorithm = pool.Algorithm;
            lastCoin = pool.Coin;
        }

        minerInstances.Add(CreateAlgorithm(uniquePool.ToArray()));

        return minerInstances.ToArray();
    }

    private IAlgorithm CreateAlgorithm(Pool[] pools)
    {
        if (pools.Length == 0) throw new Exception("Unable to create miner instances.");

        if (pools[0].Algorithm.Equals("Xiropht_Centralized_Solo", StringComparison.OrdinalIgnoreCase) ||
            pools[0].Algorithm.Equals("Xenophyte_Centralized_Solo", StringComparison.OrdinalIgnoreCase))
        {
            return new XenophyteCentralizedAlgorithm(_loggerFactory.CreateLogger(nameof(XenophyteCentralizedAlgorithm)), _options, pools[0]);
        }

        throw new NotImplementedException("Algorithm not implemented.");
    }
}