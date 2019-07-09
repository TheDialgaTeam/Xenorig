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

        public LoggerService(Program program, FilePathService filePathService)
        {
            Program = program;
            FilePathService = filePathService;
        }

        public void Initialize()
        {
            SemaphoreSlim = new SemaphoreSlim(1, 1);
        }

        public void LogMessage(string message, ConsoleColor consoleColor = ConsoleColor.White)
        {
            var semaphoreSlim = SemaphoreSlim;

            try
            {
                semaphoreSlim.Wait(Program.CancellationTokenSource.Token);
            }
            catch (Exception)
            {
                return;
            }

            try
            {
                var outWriter = System.Console.Out;

                System.Console.ForegroundColor = ConsoleColor.Gray;
                outWriter.Write($"{DateTime.UtcNow:s} ");

                System.Console.ForegroundColor = consoleColor;
                outWriter.WriteLine(message);
                outWriter.Flush();
            }
            catch (Exception ex)
            {
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.Error.WriteLine(ex.ToString());
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public void LogMessage(IEnumerable<ConsoleMessage> consoleMessages)
        {
            var semaphoreSlim = SemaphoreSlim;

            try
            {
                semaphoreSlim.Wait(Program.CancellationTokenSource.Token);
            }
            catch (Exception)
            {
                return;
            }

            try
            {
                var outWriter = System.Console.Out;

                foreach (var consoleMessage in consoleMessages)
                {
                    if (consoleMessage.IncludeDateTime)
                    {
                        System.Console.ForegroundColor = ConsoleColor.Gray;
                        outWriter.Write($"{DateTime.UtcNow:s} ");
                    }

                    System.Console.ForegroundColor = consoleMessage.Color;
                    outWriter.Write(consoleMessage.Message);
                    outWriter.Flush();
                }
            }
            catch (Exception ex)
            {
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.Error.WriteLine(ex.ToString());
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public async Task LogMessageAsync(string message, ConsoleColor consoleColor = ConsoleColor.White)
        {
            var semaphoreSlim = SemaphoreSlim;

            try
            {
                await semaphoreSlim.WaitAsync(Program.CancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception)
            {
                return;
            }

            try
            {
                var outWriter = System.Console.Out;

                System.Console.ForegroundColor = ConsoleColor.Gray;
                await outWriter.WriteAsync($"{DateTime.UtcNow:s} ").ConfigureAwait(false);

                System.Console.ForegroundColor = consoleColor;
                await outWriter.WriteLineAsync(message).ConfigureAwait(false);
                await outWriter.FlushAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Console.ForegroundColor = ConsoleColor.Red;
                await System.Console.Error.WriteLineAsync(ex.ToString()).ConfigureAwait(false);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public async Task LogMessageAsync(IEnumerable<ConsoleMessage> consoleMessages)
        {
            var semaphoreSlim = SemaphoreSlim;

            try
            {
                await semaphoreSlim.WaitAsync(Program.CancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception)
            {
                return;
            }

            try
            {
                var outWriter = System.Console.Out;

                foreach (var consoleMessage in consoleMessages)
                {
                    if (consoleMessage.IncludeDateTime)
                    {
                        System.Console.ForegroundColor = ConsoleColor.Gray;
                        await outWriter.WriteAsync($"{DateTime.UtcNow:s} ").ConfigureAwait(false);
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
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public void LogErrorMessage(Exception exception)
        {
            var semaphoreSlim = SemaphoreSlim;
            var exceptionMessage = exception.ToString();

            try
            {
                semaphoreSlim.WaitAsync(Program.CancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception)
            {
                return;
            }

            try
            {
                var outWriter = System.Console.Out;

                System.Console.ForegroundColor = ConsoleColor.Gray;
                outWriter.Write($"{DateTime.UtcNow:s} ");

                System.Console.ForegroundColor = ConsoleColor.Red;
                outWriter.WriteLine(exceptionMessage);
                outWriter.Flush();

                using (var errorWriter = new StreamWriter(new FileStream(FilePathService.ConsoleLogFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
                {
                    errorWriter.WriteLine($"{DateTime.UtcNow:s} {exceptionMessage}");
                    errorWriter.Flush();
                }
            }
            catch (Exception ex)
            {
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.Error.WriteLineAsync(ex.ToString());
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public async Task LogErrorMessageAsync(Exception exception)
        {
            var semaphoreSlim = SemaphoreSlim;
            var exceptionMessage = exception.ToString();

            try
            {
                await semaphoreSlim.WaitAsync(Program.CancellationTokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception)
            {
                return;
            }

            try
            {
                var outWriter = System.Console.Out;

                System.Console.ForegroundColor = ConsoleColor.Gray;
                await outWriter.WriteAsync($"{DateTime.UtcNow:s} ").ConfigureAwait(false);

                System.Console.ForegroundColor = ConsoleColor.Red;
                await outWriter.WriteLineAsync(exceptionMessage).ConfigureAwait(false);
                await outWriter.FlushAsync().ConfigureAwait(false);

                using (var errorWriter = new StreamWriter(new FileStream(FilePathService.ConsoleLogFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
                {
                    await errorWriter.WriteLineAsync($"{DateTime.UtcNow:s} {exceptionMessage}").ConfigureAwait(false);
                    await errorWriter.FlushAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                System.Console.ForegroundColor = ConsoleColor.Red;
                await System.Console.Error.WriteLineAsync(ex.ToString()).ConfigureAwait(false);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public void Dispose()
        {
            SemaphoreSlim?.Dispose();
        }
    }
}