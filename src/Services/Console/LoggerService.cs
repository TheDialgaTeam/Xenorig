using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheDialgaTeam.Microsoft.Extensions.DependencyInjection;
using TheDialgaTeam.Xiropht.Xirorig.Console;

namespace TheDialgaTeam.Xiropht.Xirorig.Services.Console
{
    public sealed class LoggerService : IInitializable
    {
        private ConcurrentQueue<List<ConsoleMessage>> LogsToProcess { get; set; }

        public void Initialize()
        {
            LogsToProcess = new ConcurrentQueue<List<ConsoleMessage>>();

            Program.TasksToAwait.Add(Task.Run(async () =>
            {
                var cancellationTokenSource = Program.CancellationTokenSource;

                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    while (LogsToProcess.TryDequeue(out var consoleMessages))
                    {
                        try
                        {
                            var outWriter = System.Console.Out;

                            foreach (var consoleMessage in consoleMessages)
                            {
                                if (consoleMessage.IncludeDateTime)
                                {
                                    System.Console.ForegroundColor = ConsoleColor.Gray;
                                    await outWriter.WriteAsync($"{DateTime.Now:s} ").ConfigureAwait(false);
                                }

                                System.Console.ForegroundColor = consoleMessage.Color;
                                await outWriter.WriteAsync(consoleMessage.Message).ConfigureAwait(false);
                                await outWriter.FlushAsync().ConfigureAwait(false);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Console.ForegroundColor = ConsoleColor.Red;
                            await System.Console.Error.WriteLineAsync(ex.ToString()).ConfigureAwait(false);
                        }
                    }

                    await Task.Delay(1, cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }, Program.CancellationTokenSource.Token));
        }

        public void LogMessage(string message)
        {
            LogsToProcess.Enqueue(new ConsoleMessageBuilder().WriteLine(message).Build());
        }

        public void LogMessage(string message, ConsoleColor color)
        {
            LogsToProcess.Enqueue(new ConsoleMessageBuilder().WriteLine(message, color).Build());
        }

        public void LogMessage(string message, bool includeDateTime)
        {
            LogsToProcess.Enqueue(new ConsoleMessageBuilder().WriteLine(message, includeDateTime).Build());
        }

        public void LogMessage(string message, ConsoleColor color, bool includeDateTime)
        {
            LogsToProcess.Enqueue(new ConsoleMessageBuilder().WriteLine(message, color, includeDateTime).Build());
        }

        public void LogMessage(List<ConsoleMessage> consoleMessages)
        {
            LogsToProcess.Enqueue(consoleMessages);
        }
    }
}