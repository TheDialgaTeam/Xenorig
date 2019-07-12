using System;
using System.Threading.Tasks;
using TheDialgaTeam.Microsoft.Extensions.DependencyInjection;
using TheDialgaTeam.Xiropht.Xirorig.Console;
using TheDialgaTeam.Xiropht.Xirorig.Mining.Pool;
using TheDialgaTeam.Xiropht.Xirorig.Mining.Solo;
using TheDialgaTeam.Xiropht.Xirorig.Services.Mining;

namespace TheDialgaTeam.Xiropht.Xirorig.Services.Console
{
    public sealed class ConsoleCommandService : IInitializable
    {
        private LoggerService LoggerService { get; }

        private MiningService MiningService { get; }

        public ConsoleCommandService(LoggerService loggerService, MiningService miningService)
        {
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

            Program.TasksToAwait.Add(Task.Run(async () =>
            {
                var cancellationTokenSource = Program.CancellationTokenSource;
                var loggerService = LoggerService;
                var miningService = MiningService;

                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    if (System.Console.KeyAvailable)
                    {
                        var keyPressed = System.Console.ReadKey(true);

                        if (keyPressed.KeyChar == 'h')
                        {
                            decimal average10SecondsSum = 0, average60SecondsSum = 0, average15MinutesSum = 0;

                            var tableBuilder = new ConsoleMessageBuilder()
                                .WriteLine("| THREAD | 10s H/s | 60s H/s | 15m H/s |");

                            var miner = miningService.Miner;

                            for (var i = 0; i < miner.Average10SecondsHashesCalculated.Count; i++)
                                average10SecondsSum += miner.Average10SecondsHashesCalculated[i];

                            for (var i = 0; i < miner.Average60SecondsHashesCalculated.Count; i++)
                                average60SecondsSum += miner.Average60SecondsHashesCalculated[i];

                            for (var i = 0; i < miner.Average15MinutesHashesCalculated.Count; i++)
                                average15MinutesSum += miner.Average15MinutesHashesCalculated[i];

                            for (var i = 0; i < miner.Average10SecondsHashesCalculated.Count; i++)
                                tableBuilder.WriteLine($"| {i.ToString().PadLeft(6)} | {miner.Average10SecondsHashesCalculated[i].ToString("F0").PadLeft(7)} | {miner.Average60SecondsHashesCalculated[i].ToString("F0").PadLeft(7)} | {miner.Average15MinutesHashesCalculated[i].ToString("F0").PadLeft(7)} |");

                            tableBuilder.Write("speed 10s/60s/15m ")
                                .Write($"{average10SecondsSum:F0} ", ConsoleColor.Cyan, false)
                                .Write($"{average60SecondsSum:F0} ", ConsoleColor.DarkCyan, false)
                                .Write($"{average15MinutesSum:F0} ", ConsoleColor.DarkCyan, false)
                                .Write("H/s ", ConsoleColor.Cyan, false)
                                .Write("max ", false)
                                .WriteLine($"{miner.MaxHashes:F0} H/s", ConsoleColor.Cyan, false);

                            loggerService.LogMessage(tableBuilder.Build());
                        }
                        else if (keyPressed.KeyChar == 's')
                        {
                            loggerService.LogMessage(new ConsoleMessageBuilder()
                                .Write("Good Shares: ", ConsoleColor.Green)
                                .Write(miningService.TotalGoodSharesSubmitted.ToString("F0"), false)
                                .Write(", ", false)
                                .Write("Bad Shares: ", ConsoleColor.Red, false)
                                .WriteLine(miningService.TotalBadSharesSubmitted.ToString("F0"), false)
                                .Build());
                        }
                        else if (keyPressed.KeyChar == 'j')
                        {
                            var listener = miningService.Listener;
                            var miner = miningService.Miner;

                            switch (listener)
                            {
                                case SoloListener _:
                                    loggerService.LogMessage(new ConsoleMessageBuilder()
                                        .Write("current job ", ConsoleColor.Magenta, true)
                                        .WriteLine($"from {listener.Host}:{listener.Port} diff {miner.BlockDifficulty} algo {miner.JobMethodName} height {miner.BlockId}", false)
                                        .Build());
                                    break;

                                case PoolListener _:
                                    loggerService.LogMessage(new ConsoleMessageBuilder()
                                        .Write("current job ", ConsoleColor.Magenta, true)
                                        .WriteLine($"from {listener.Host}:{listener.Port} diff {miner.JobDifficulty}/{miner.BlockDifficulty} algo {miner.JobMethodName} height {miner.BlockId}", false)
                                        .Build());
                                    break;
                            }
                        }
                    }

                    await Task.Delay(1, cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }, Program.CancellationTokenSource.Token));

            LoggerService.LogMessage(new ConsoleMessageBuilder()
                .Write(" * ", ConsoleColor.Green, false)
                .Write("COMMANDS     ", false)
                .Write("h", ConsoleColor.Magenta, false)
                .Write("ashrate", false)
                .Write(", ", false)
                .Write("s", ConsoleColor.Magenta, false)
                .Write("tats", false)
                .Write(", ", false)
                .Write("j", ConsoleColor.Magenta, false)
                .WriteLine("ob", false)
                .Build());
        }
    }
}
