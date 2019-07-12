using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using TheDialgaTeam.Microsoft.Extensions.DependencyInjection;
using TheDialgaTeam.Xiropht.Xirorig.Services.Bootstrap;
using TheDialgaTeam.Xiropht.Xirorig.Services.Console;
using TheDialgaTeam.Xiropht.Xirorig.Services.IO;
using TheDialgaTeam.Xiropht.Xirorig.Services.Mining;
using TheDialgaTeam.Xiropht.Xirorig.Services.Setting;

namespace TheDialgaTeam.Xiropht.Xirorig
{
    public static class Program
    {
        public static CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();

        public static List<Task> TasksToAwait { get; } = new List<Task>();

        public static ServiceProvider ServiceProvider { get; private set; }

        public static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddInterfacesAndSelfAsSingleton<FilePathService>();
            serviceCollection.AddInterfacesAndSelfAsSingleton<LoggerService>();
            serviceCollection.AddInterfacesAndSelfAsSingleton<BootstrapService>();
            serviceCollection.AddInterfacesAndSelfAsSingleton<ConfigService>();
            serviceCollection.AddInterfacesAndSelfAsSingleton<MiningService>();
            serviceCollection.AddInterfacesAndSelfAsSingleton<ConsoleCommandService>();

            ServiceProvider = serviceCollection.BuildServiceProvider();

            try
            {
                ServiceProvider.InitializeServices();
                ServiceProvider.LateInitializeServices();

                Task.WaitAll(TasksToAwait.ToArray());

                ServiceProvider.DisposeServices();
                Dispose();

                Environment.Exit(0);
            }
            catch (AggregateException ex)
            {
                var loggerService = ServiceProvider?.GetService<LoggerService>();

                if (loggerService != null)
                {
                    foreach (var exception in ex.InnerExceptions)
                    {
                        if (exception is OperationCanceledException)
                            continue;

                        loggerService.LogMessage(exception.ToString(), ConsoleColor.Red, false);
                        loggerService.LogMessage("Press Enter/Return to exit...", false);
                        System.Console.ReadLine();
                    }
                }

                ExitWithFault();
            }
            catch (Exception ex)
            {
                var loggerService = ServiceProvider?.GetService<LoggerService>();

                if (loggerService != null)
                {
                    loggerService.LogMessage(ex.ToString(), ConsoleColor.Red, false);
                    loggerService.LogMessage("Press Enter/Return to exit...", false);
                    System.Console.ReadLine();
                }

                ExitWithFault();
            }
        }

        private static void ExitWithFault()
        {
            CancellationTokenSource?.Cancel();
            ServiceProvider?.DisposeServices();
            Dispose();

            Environment.Exit(1);
        }

        private static void Dispose()
        {
            foreach (var task in TasksToAwait)
                task?.Dispose();

            CancellationTokenSource?.Dispose();
            ServiceProvider?.Dispose();
        }
    }
}