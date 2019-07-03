using System;
using System.Threading.Tasks;
using TheDialgaTeam.Microsoft.Extensions.DependencyInjection;
using TheDialgaTeam.Xiropht.Xirorig.Console;
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
                            var tableBuilder = new ConsoleMessageBuilder()
                                .WriteLine("| THREAD | 10s H/s | 60s H/s | 15m H/s |", false);

                            for (var i = 0; i < PoolService.PoolMiner.Average10SecondsHashesCalculated.Count; i++)
                                average10SecondsSum += PoolService.PoolMiner.Average10SecondsHashesCalculated[i];

                            for (var i = 0; i < PoolService.PoolMiner.Average60SecondsHashesCalculated.Count; i++)
                                average60SecondsSum += PoolService.PoolMiner.Average60SecondsHashesCalculated[i];

                            for (var i = 0; i < PoolService.PoolMiner.Average15MinutesHashesCalculated.Count; i++)
                                average15MinutesSum += PoolService.PoolMiner.Average15MinutesHashesCalculated[i];

                            for (var i = 0; i < PoolService.PoolMiner.Average10SecondsHashesCalculated.Count; i++)
                                tableBuilder.WriteLine($"| {i.ToString().PadLeft(6)} | {PoolService.PoolMiner.Average10SecondsHashesCalculated[i].ToString("F1").PadLeft(7)} | {PoolService.PoolMiner.Average60SecondsHashesCalculated[i].ToString("F1").PadLeft(7)} | {PoolService.PoolMiner.Average15MinutesHashesCalculated[i].ToString("F1").PadLeft(7)} |", false);

                            await LoggerService.LogMessageAsync(tableBuilder.Build()).ConfigureAwait(false);
                            await LoggerService.LogMessageAsync(new ConsoleMessageBuilder()
                                .Write("speed 10s/60s/15m ", true)
                                .Write($"{average10SecondsSum:F1} ", ConsoleColor.Cyan, false)
                                .Write($"{average60SecondsSum:F1} ", ConsoleColor.DarkCyan, false)
                                .Write($"{average15MinutesSum:F1} ", ConsoleColor.DarkCyan, false)
                                .Write("H/s ", ConsoleColor.Cyan, false)
                                .Write("max ", false)
                                .WriteLine($"{PoolService.PoolMiner.MaxHashes:F1} H/s", ConsoleColor.Cyan, false)
                                .Build()).ConfigureAwait(false);
                        }
                    }

                    await Task.Delay(1).ConfigureAwait(false);
                }
            }, Program.CancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current));

            var consoleMessages = new ConsoleMessageBuilder()
                .Write(" * ", ConsoleColor.Green, false)
                .Write("COMMANDS".PadRight(13), false)
                .Write("h", ConsoleColor.Magenta, false)
                .Write("ashrate", false)
                .WriteLine("", false);

            LoggerService.LogMessage(consoleMessages.Build());
        }
    }
}
