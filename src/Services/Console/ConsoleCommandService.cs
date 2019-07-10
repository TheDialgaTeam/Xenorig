using System;
using System.Threading.Tasks;
using TheDialgaTeam.Microsoft.Extensions.DependencyInjection;
using TheDialgaTeam.Xiropht.Xirorig.Console;
using TheDialgaTeam.Xiropht.Xirorig.Services.Mining;

namespace TheDialgaTeam.Xiropht.Xirorig.Services.Console
{
    public sealed class ConsoleCommandService : IInitializable
    {
        private Program Program { get; }

        private LoggerService LoggerService { get; }

        private MiningService MiningService { get; }

        public ConsoleCommandService(Program program, LoggerService loggerService, MiningService miningService)
        {
            Program = program;
            LoggerService = loggerService;
            MiningService = miningService;
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

                            for (var i = 0; i < MiningService.Miner.Average10SecondsHashesCalculated.Count; i++)
                                average10SecondsSum += MiningService.Miner.Average10SecondsHashesCalculated[i];

                            for (var i = 0; i < MiningService.Miner.Average60SecondsHashesCalculated.Count; i++)
                                average60SecondsSum += MiningService.Miner.Average60SecondsHashesCalculated[i];

                            for (var i = 0; i < MiningService.Miner.Average15MinutesHashesCalculated.Count; i++)
                                average15MinutesSum += MiningService.Miner.Average15MinutesHashesCalculated[i];

                            for (var i = 0; i < MiningService.Miner.Average10SecondsHashesCalculated.Count; i++)
                                tableBuilder.WriteLine($"| {i.ToString().PadLeft(6)} | {MiningService.Miner.Average10SecondsHashesCalculated[i].ToString("F0").PadLeft(7)} | {MiningService.Miner.Average60SecondsHashesCalculated[i].ToString("F0").PadLeft(7)} | {MiningService.Miner.Average15MinutesHashesCalculated[i].ToString("F0").PadLeft(7)} |", false);

                            await LoggerService.LogMessageAsync(tableBuilder.Build()).ConfigureAwait(false);
                            await LoggerService.LogMessageAsync(new ConsoleMessageBuilder()
                                .Write("speed 10s/60s/15m ", true)
                                .Write($"{average10SecondsSum:F0} ", ConsoleColor.Cyan, false)
                                .Write($"{average60SecondsSum:F0} ", ConsoleColor.DarkCyan, false)
                                .Write($"{average15MinutesSum:F0} ", ConsoleColor.DarkCyan, false)
                                .Write("H/s ", ConsoleColor.Cyan, false)
                                .Write("max ", false)
                                .WriteLine($"{MiningService.Miner.MaxHashes:F0} H/s", ConsoleColor.Cyan, false)
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
                .WriteLine("ashrate", false);

            LoggerService.LogMessage(consoleMessages.Build());
        }
    }
}
