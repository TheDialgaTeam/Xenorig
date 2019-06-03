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

                        if (keyPressed.KeyChar == 'h')
                        {
                            decimal average10SecondsSum = 0, average60SecondsSum = 0, average15MinutesSum = 0;
                            var tableBuilder = new ConsoleMessageBuilder();

                            foreach (var hash in PoolService.PoolMiner.Average10SecondsHashesCalculated)
                                average10SecondsSum += hash.Value;

                            foreach (var hash in PoolService.PoolMiner.Average60SecondsHashesCalculated)
                                average60SecondsSum += hash.Value;

                            foreach (var hash in PoolService.PoolMiner.Average15MinutesHashesCalculated)
                                average15MinutesSum += hash.Value;

                            await LoggerService.LogMessageAsync(new ConsoleMessageBuilder()
                                .Write("speed 10s/60s/15m ", includeDateTime: true)
                                .Write($"{average10SecondsSum:F1} ", ConsoleColor.Cyan)
                                .Write($"{average60SecondsSum:F1} ", ConsoleColor.DarkCyan)
                                .Write($"{average15MinutesSum:F1} ", ConsoleColor.DarkCyan)
                                .Write("H/s ", ConsoleColor.Cyan)
                                .Write("max ")
                                .WriteLine($"{PoolService.PoolMiner.MaxHashes:F1} H/s", ConsoleColor.Cyan, false)
                                .Build()).ConfigureAwait(false);
                        }
                    }

                    await Task.Delay(1).ConfigureAwait(false);
                }
            }, Program.CancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current));

            var consoleMessages = new ConsoleMessageBuilder()
                .Write(" * ", ConsoleColor.Green)
                .Write("COMMANDS".PadRight(13))
                .Write("h", ConsoleColor.Magenta)
                .Write("ashrate")
                .WriteLine("", includeDateTime: false);

            LoggerService.LogMessage(consoleMessages.Build());
        }
    }
}
