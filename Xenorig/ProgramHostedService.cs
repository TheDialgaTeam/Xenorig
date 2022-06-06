using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xenorig.Algorithms.Xenophyte.Centralized;
using Xenorig.Utilities;

namespace Xenorig;

internal class ProgramHostedService : IHostedService
{
    private readonly ILogger<ProgramHostedService> _logger;
    private readonly ProgramContext _programContext;
    private readonly ILoggerFactory _loggerFactory;

    public ProgramHostedService(ILogger<ProgramHostedService> logger, ProgramContext programContext, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _programContext = programContext;
        _loggerFactory = loggerFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Console.Title = $"{ApplicationUtility.Name} v{ApplicationUtility.Version} ({ApplicationUtility.FrameworkVersion})";

        Logger.PrintAbout(_logger, "About", ApplicationUtility.Name, ApplicationUtility.Version, ApplicationUtility.FrameworkVersion);
        Logger.PrintCpu(_logger, "CPU", CpuInformationUtility.ProcessorName, CpuInformationUtility.ProcessorInstructionSetsSupported);
        Logger.PrintCpuCont(_logger, string.Empty, CpuInformationUtility.ProcessorL2Cache / 1024.0 / 1024.0, CpuInformationUtility.ProcessorL3Cache / 1024.0 / 1024.0, CpuInformationUtility.ProcessorCoreCount, CpuInformationUtility.ProcessorThreadCount);
        Logger.PrintDonatePercentage(_logger, "DONATE", 0);
        Logger.PrintCommand(_logger, "COMMANDS");

        var pools = _programContext.Options.GetPools();

        for (var i = 0; i < pools.Length; i++)
        {
            var pool = pools[i];
            Logger.PrintPool(_logger, $"SOLO #{i}", pool.GetUrl(), pool.GetAlgorithm());
        }

        _logger.LogInformation(string.Empty);

        var test = new XenophyteCentralizedAlgorithm(_programContext, _loggerFactory.CreateLogger<XenophyteCentralizedAlgorithm>(), pools);
        await test.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}