﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xenolib.Utilities;
using Xenorig.Algorithms;
using Xenorig.Algorithms.Xenophyte.Centralized.Solo;
using Xenorig.Options;

namespace Xenorig;

internal sealed class ConsoleService : BackgroundService
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

        for (var i = 0; i < _options.Pools.Length; i++)
        {
            var pool = _options.Pools[i];
            Logger.PrintPool(_logger, $"POOL #{i + 1}", pool.Url, pool.Algorithm);
        }

        Logger.PrintEmpty(_logger);
        
        _minerInstances = CreateMinerInstances();
        if (_minerInstances.Length == 0) return;

        await _minerInstances[_currentIndex].StartAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var readKeyTask = Task.Factory.StartNew(() => Console.ReadKey(true), stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
            var taskCompleted = await Task.WhenAny(readKeyTask, Task.Delay(Timeout.Infinite, stoppingToken));
            if (taskCompleted != readKeyTask) return;

            var keyPressed = await readKeyTask;

            switch (keyPressed.Key)
            {
                case ConsoleKey.H:
                    _minerInstances[_currentIndex].PrintHashrate();
                    break;

                case ConsoleKey.S:
                    _minerInstances[_currentIndex].PrintStats();
                    break;

                case ConsoleKey.J:
                    _minerInstances[_currentIndex].PrintCurrentJob();
                    break;
            }
        }
    }

    private IAlgorithm[] CreateMinerInstances()
    {
        return _options.Pools.Select(CreateAlgorithm).ToArray();
    }

    private IAlgorithm CreateAlgorithm(Pool pool)
    {
        if (pool.Algorithm.Equals("Xiropht_Centralized_Solo", StringComparison.OrdinalIgnoreCase) ||
            pool.Algorithm.Equals("Xenophyte_Centralized_Solo", StringComparison.OrdinalIgnoreCase))
        {
            return new XenophyteCentralizedSoloAlgorithm(_loggerFactory.CreateLogger(nameof(XenophyteCentralizedSoloAlgorithm)), _options, pool);
        }

        throw new NotImplementedException("Algorithm not implemented.");
    }
}