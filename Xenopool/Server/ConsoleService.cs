using Xenolib.Utilities;
using Xenopool.Server.Networking.RpcWallet;

namespace Xenopool.Server;

public class ConsoleService : BackgroundService
{
    private readonly ILogger<ConsoleService> _logger;
    private readonly RpcWalletNetwork _rpcWalletNetwork;

    public ConsoleService(ILogger<ConsoleService> logger, RpcWalletNetwork rpcWalletNetwork)
    {
        _logger = logger;
        _rpcWalletNetwork = rpcWalletNetwork;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.Title = $"{ApplicationUtility.Name} v{ApplicationUtility.Version} ({ApplicationUtility.FrameworkVersion})";
        
        Logger.PrintAbout(_logger, "About", ApplicationUtility.Name, ApplicationUtility.Version, ApplicationUtility.FrameworkVersion);
        Logger.PrintCpu(_logger, "CPU", CpuInformationUtility.ProcessorName, CpuInformationUtility.ProcessorInstructionSetsSupported);
        Logger.PrintCpuCont(_logger, string.Empty, CpuInformationUtility.ProcessorL2Cache / 1024.0 / 1024.0, CpuInformationUtility.ProcessorL3Cache / 1024.0 / 1024.0, CpuInformationUtility.ProcessorCoreCount, CpuInformationUtility.ProcessorThreadCount);
        Logger.PrintEmpty(_logger);

        if (!await _rpcWalletNetwork.CheckIfWalletAddressExistAsync(stoppingToken))
        {
            Logger.PrintMessage(_logger, "NO");
            return;
        }
        
        while (!stoppingToken.IsCancellationRequested)
        {
            var readKeyTask = Task.Factory.StartNew(Console.ReadLine, stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
            var taskCompleted = await Task.WhenAny(readKeyTask, Task.Delay(Timeout.Infinite, stoppingToken));
            if (taskCompleted != readKeyTask) return;

            var keyPressed = await readKeyTask;
            if (keyPressed == null) break;
        }
    }
}