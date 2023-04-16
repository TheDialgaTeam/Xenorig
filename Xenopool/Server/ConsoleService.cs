using Microsoft.EntityFrameworkCore;
using Xenolib.Utilities;
using Xenopool.Server.Database;
using Xenopool.Server.Networking.RpcWallet;
using Xenopool.Server.Networking.SoloMining;

namespace Xenopool.Server;

public class ConsoleService : BackgroundService
{
    private readonly ILogger<ConsoleService> _logger;
    private readonly RpcWalletNetwork _rpcWalletNetwork;
    private readonly SoloMiningNetwork _soloMiningNetwork;
    private readonly IDbContextFactory<SqliteDatabaseContext> _dbContextFactory;

    public ConsoleService(ILogger<ConsoleService> logger, RpcWalletNetwork rpcWalletNetwork, SoloMiningNetwork soloMiningNetwork, IDbContextFactory<SqliteDatabaseContext> dbContextFactory)
    {
        _logger = logger;
        _rpcWalletNetwork = rpcWalletNetwork;
        _soloMiningNetwork = soloMiningNetwork;
        _dbContextFactory = dbContextFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Console.Title = $"{ApplicationUtility.Name} v{ApplicationUtility.Version} ({ApplicationUtility.FrameworkVersion})";
        
        Logger.PrintAbout(_logger, "About", ApplicationUtility.Name, ApplicationUtility.Version, ApplicationUtility.FrameworkVersion);
        Logger.PrintCpu(_logger, "CPU", CpuInformationUtility.ProcessorName, CpuInformationUtility.ProcessorInstructionSetsSupported);
        Logger.PrintCpuCont(_logger, string.Empty, CpuInformationUtility.ProcessorL2Cache / 1024.0 / 1024.0, CpuInformationUtility.ProcessorL3Cache / 1024.0 / 1024.0, CpuInformationUtility.ProcessorCoreCount, CpuInformationUtility.ProcessorThreadCount);
        Logger.PrintEmpty(_logger);

        await using var sqliteDatabaseContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        await sqliteDatabaseContext.Database.MigrateAsync(cancellationToken);

        if (!await _rpcWalletNetwork.CheckIfWalletAddressExistAsync(cancellationToken))
        {
            Logger.PrintWalletAddressNotExists(_logger);
            return;
        }

        await _soloMiningNetwork.StartAsync(cancellationToken);
        
        while (!cancellationToken.IsCancellationRequested)
        {
            var readKeyTask = Task.Factory.StartNew(Console.ReadLine, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
            var taskCompleted = await Task.WhenAny(readKeyTask, Task.Delay(Timeout.Infinite, cancellationToken));
            if (taskCompleted != readKeyTask) return;

            var keyPressed = await readKeyTask;
            if (keyPressed == null) break;
        }
    }
}