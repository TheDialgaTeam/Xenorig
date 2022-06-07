using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xenorig.Algorithms;
using Xenorig.Algorithms.Xenophyte.Centralized;
using Xenorig.Options;
using Xenorig.Utilities;

namespace Xenorig;

internal class ProgramHostedService : IHostedService, IDisposable
{
    private readonly ILogger<ProgramHostedService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    private readonly IDisposable? _optionsDisposable;
    private XenorigOptions _options;

    private readonly Thread _consoleThread;
    private IAlgorithm? _algorithm;

    public ProgramHostedService(ILogger<ProgramHostedService> logger, ILoggerFactory loggerFactory, IOptionsMonitor<XenorigOptions> optionsMonitor, IHostApplicationLifetime hostApplicationLifetime)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _hostApplicationLifetime = hostApplicationLifetime;

        _optionsDisposable = optionsMonitor.OnChange(XenorigOptionsListener);
        _options = optionsMonitor.CurrentValue;

        _consoleThread = new Thread(ConsoleThreadStart) { IsBackground = true };
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Console.Title = $"{ApplicationUtility.Name} v{ApplicationUtility.Version} ({ApplicationUtility.FrameworkVersion})";

        Logger.PrintAbout(_logger, "About", ApplicationUtility.Name, ApplicationUtility.Version, ApplicationUtility.FrameworkVersion);
        Logger.PrintCpu(_logger, "CPU", CpuInformationUtility.ProcessorName, CpuInformationUtility.ProcessorInstructionSetsSupported);
        Logger.PrintCpuCont(_logger, string.Empty, CpuInformationUtility.ProcessorL2Cache / 1024.0 / 1024.0, CpuInformationUtility.ProcessorL3Cache / 1024.0 / 1024.0, CpuInformationUtility.ProcessorCoreCount, CpuInformationUtility.ProcessorThreadCount);
        Logger.PrintDonatePercentage(_logger, "DONATE", 0);
        Logger.PrintCommand(_logger, "COMMANDS");

        var pools = _options.GetPools();

        for (var i = 0; i < pools.Length; i++)
        {
            var pool = pools[i];
            Logger.PrintPool(_logger, $"SOLO #{i}", pool.GetUrl(), pool.GetAlgorithm());
        }

        _logger.LogInformation(string.Empty);

        _algorithm = new XenophyteCentralizedAlgorithm(_options, _loggerFactory.CreateLogger<XenophyteCentralizedAlgorithm>(), pools);
        await _algorithm.StartAsync(cancellationToken);

        _consoleThread.Start();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_algorithm == null) return;
        await _algorithm.StopAsync(cancellationToken);
    }

    private void ConsoleThreadStart()
    {
        while (!_hostApplicationLifetime.ApplicationStopping.IsCancellationRequested)
        {
            var keyPressed = Console.ReadKey(true);

            switch (keyPressed.Key)
            {
                case ConsoleKey.H:
                    _algorithm?.PrintHashrate();
                    break;

                case ConsoleKey.S:
                    _algorithm?.PrintStats();
                    break;

                case ConsoleKey.J:
                    _algorithm?.PrintCurrentJob();
                    break;
            }
        }
    }

    private void XenorigOptionsListener(XenorigOptions obj)
    {
        _options = obj;
    }

    public void Dispose()
    {
        _optionsDisposable?.Dispose();
    }
}