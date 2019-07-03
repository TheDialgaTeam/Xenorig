using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TheDialgaTeam.Microsoft.Extensions.DependencyInjection;
using TheDialgaTeam.Xiropht.Xirorig.Console;
using TheDialgaTeam.Xiropht.Xirorig.Services.IO;

namespace TheDialgaTeam.Xiropht.Xirorig.Services.Console
{
    public sealed class LoggerService : IInitializable, IDisposable
    {
        private Program Program { get; }

        private FilePathService FilePathService { get; }

        private SemaphoreSlim SemaphoreSlim { get; set; }

        private StreamWriter StreamWriter { get; set; }

        public LoggerService(Program program, FilePathService filePathService)
        {
            Program = program;
            FilePathService = filePathService;
        }

        public void Initialize()
        {
            SemaphoreSlim = new SemaphoreSlim(1, 1);
            StreamWriter = new StreamWriter(new FileStream(FilePathService.ConsoleLogFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
        }

        public void LogMessage(string message, ConsoleColor consoleColor = ConsoleColor.White)
        {
            LogMessageAsync(message, consoleColor).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public void LogMessage(IEnumerable<ConsoleMessage> consoleMessages)
        {
            LogMessageAsync(consoleMessages).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public async Task LogMessageAsync(string message, ConsoleColor consoleColor = ConsoleColor.White)
        {
            await LogMessageAsync(System.Console.Out, consoleColor, message).ConfigureAwait(false);
        }

        public async Task LogMessageAsync(IEnumerable<ConsoleMessage> consoleMessages)
        {
            await LogMessageAsync(System.Console.Out, consoleMessages).ConfigureAwait(false);
        }

        public void LogErrorMessage(Exception exception)
        {
            LogErrorMessageAsync(exception).GetAwaiter().GetResult();
        }

        public async Task LogErrorMessageAsync(Exception exception)
        {
            await LogMessageAsync(System.Console.Error, ConsoleColor.Red, exception.ToString()).ConfigureAwait(false);
        }

        private async Task LogMessageAsync(TextWriter writer, ConsoleColor consoleColor, string message)
        {
            try
            {
                await SemaphoreSlim.WaitAsync(Program.CancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception)
            {
                return;
            }

            try
            {
                System.Console.ForegroundColor = ConsoleColor.Gray;
                await writer.WriteAsync($"{DateTime.UtcNow:s} ").ConfigureAwait(false);

                System.Console.ForegroundColor = consoleColor;
                await writer.WriteLineAsync(message).ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);

                if (writer != System.Console.Error)
                    return;

                await StreamWriter.WriteLineAsync($"{DateTime.UtcNow:s} {message}").ConfigureAwait(false);
                await StreamWriter.FlushAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Console.ForegroundColor = ConsoleColor.Red;
                await System.Console.Error.WriteLineAsync(ex.ToString()).ConfigureAwait(false);
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        private async Task LogMessageAsync(TextWriter writer, IEnumerable<ConsoleMessage> consoleMessages)
        {
            try
            {
                await SemaphoreSlim.WaitAsync(Program.CancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception)
            {
                return;
            }

            try
            {
                foreach (var consoleMessage in consoleMessages)
                {
                    if (consoleMessage.IncludeDateTime)
                    {
                        System.Console.ForegroundColor = ConsoleColor.Gray;
                        await writer.WriteAsync($"{DateTime.UtcNow:s} ").ConfigureAwait(false);
                    }

                    System.Console.ForegroundColor = consoleMessage.Color;
                    await writer.WriteAsync(consoleMessage.Message).ConfigureAwait(false); ;
                    await writer.FlushAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                System.Console.ForegroundColor = ConsoleColor.Red;
                await System.Console.Error.WriteLineAsync(ex.ToString()).ConfigureAwait(false);
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        public void Dispose()
        {
            SemaphoreSlim?.Dispose();
            StreamWriter?.Dispose();
        }
    }
}