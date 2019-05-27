using System;
using System.Threading.Tasks;
using TheDialgaTeam.Microsoft.Extensions.DependencyInjection;
using TheDialgaTeam.Xiropht.Xirorig.Services.Pool;

namespace TheDialgaTeam.Xiropht.Xirorig.Services.Console
{
    public sealed class ConsoleCommandService : IInitializable
    {
        private Program Program { get; }

        private LoggerService LoggerService { get; }

        private PoolService PoolService { get; }

        public ConsoleCommandService(Program program, LoggerService loggerService, PoolService poolService)
        {
            Program = program;
            LoggerService = loggerService;
            PoolService = poolService;
        }

        public void Initialize()
        {
            System.Console.CancelKeyPress += (sender, args) =>
            {
                args.Cancel = true;
                Program.CancellationTokenSource.Cancel();
            };

            Program.TasksToAwait.Add(Task.Factory.StartNew(async () =>
            {
                while (!Program.CancellationTokenSource.IsCancellationRequested)
                {
                    if (System.Console.KeyAvailable)
                    {
                        var keyPressed = System.Console.ReadKey(true);

                        if (PoolService.IsConnected && PoolService.IsLoggedIn && keyPressed.Key == ConsoleKey.S)
                            await LoggerService.LogMessageAsync($"Estimated Hashrate: {PoolService.PoolMiner.TotalHashCalculated} H/s | Good Share: {PoolService.TotalGoodSharesSubmitted} | Invalid Share: {PoolService.TotalBadSharesSubmitted}", ConsoleColor.Magenta).ConfigureAwait(false);
                    }

                    await Task.Delay(1).ConfigureAwait(false);
                }
            }, Program.CancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current));
        }
    }
}
