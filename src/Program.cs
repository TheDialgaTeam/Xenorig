using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using TheDialgaTeam.Microsoft.Extensions.DependencyInjection;
using TheDialgaTeam.Xiropht.Xirorig.Services.Bootstrap;
using TheDialgaTeam.Xiropht.Xirorig.Services.Console;
using TheDialgaTeam.Xiropht.Xirorig.Services.IO;
using TheDialgaTeam.Xiropht.Xirorig.Services.Pool;
using TheDialgaTeam.Xiropht.Xirorig.Services.Setting;

namespace TheDialgaTeam.Xiropht.Xirorig
{
    public sealed class Program : IDisposable
    {
        public CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();

        public List<Task> TasksToAwait { get; } = new List<Task>();

        public ServiceProvider ServiceProvider { get; private set; }

        public static async Task Main(string[] args)
        {
            var program = new Program();
            await program.Start(args).ConfigureAwait(false);
        }

        private async Task Start(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddInterfacesAndSelfAsSingleton(this);
            serviceCollection.AddInterfacesAndSelfAsSingleton<FilePathService>();
            serviceCollection.AddInterfacesAndSelfAsSingleton<LoggerService>();
            serviceCollection.AddInterfacesAndSelfAsSingleton<BootstrapService>();
            serviceCollection.AddInterfacesAndSelfAsSingleton<ConfigService>();
            serviceCollection.AddInterfacesAndSelfAsSingleton<PoolService>();
            serviceCollection.AddInterfacesAndSelfAsSingleton<ConsoleCommandService>();

            ServiceProvider = serviceCollection.BuildServiceProvider();

            try
            {
                ServiceProvider.InitializeServices();
                ServiceProvider.LateInitializeServices();

                Task.WaitAll(TasksToAwait.ToArray());

                ServiceProvider.DisposeServices();
                Environment.Exit(0);
            }
            catch (AggregateException ex)
            {
                var loggerService = ServiceProvider?.GetService<LoggerService>();

                if (loggerService != null)
                {
                    foreach (var exception in ex.InnerExceptions)
                    {
                        if (exception is TaskCanceledException)
                            continue;

                        await loggerService.LogErrorMessageAsync(exception).ConfigureAwait(false);
                    }
                }

                CancellationTokenSource?.Cancel();
                ServiceProvider?.DisposeServices();

                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                var loggerService = ServiceProvider?.GetService<LoggerService>();

                if (loggerService != null)
                    await loggerService.LogErrorMessageAsync(ex).ConfigureAwait(false);

                CancellationTokenSource?.Cancel();
                ServiceProvider?.DisposeServices();

                Environment.Exit(1);
            }
        }

        public void Dispose()
        {
            foreach (var task in TasksToAwait)
                task.Dispose();

            CancellationTokenSource?.Dispose();
            ServiceProvider?.Dispose();
        }
    }
}