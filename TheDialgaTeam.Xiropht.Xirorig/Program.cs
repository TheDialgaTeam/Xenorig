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
        public CancellationTokenSource CancellationTokenSource { get; private set; }

        public List<Task> TasksToAwait { get; private set; }

        public static async Task Main(string[] args)
        {
            var program = new Program();
            await program.Start(args).ConfigureAwait(false);
        }

        private async Task Start(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(this);
            serviceCollection.AddInterfacesAndSelfAsSingleton<FilePathService>();
            serviceCollection.AddInterfacesAndSelfAsSingleton<LoggerService>();
            serviceCollection.AddInterfacesAndSelfAsSingleton<BootstrapService>();
            serviceCollection.AddInterfacesAndSelfAsSingleton<ConfigService>();
            serviceCollection.AddInterfacesAndSelfAsSingleton<PoolService>();
            serviceCollection.AddInterfacesAndSelfAsSingleton<ConsoleCommandService>();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            CancellationTokenSource = new CancellationTokenSource();
            TasksToAwait = new List<Task>();

            try
            {
                serviceProvider.InitializeServices();
                Task.WaitAll(TasksToAwait.ToArray());
            }
            catch (Exception ex)
            {
                var loggerService = serviceProvider.GetService<LoggerService>();

                if (loggerService != null)
                    await loggerService.LogErrorMessageAsync(ex).ConfigureAwait(false);

                CancellationTokenSource?.Cancel();

                serviceProvider?.DisposeServices();
                serviceProvider?.Dispose();

                Environment.Exit(1);
                return;
            }

            try
            {
                serviceProvider.DisposeServices();
                serviceProvider.Dispose();
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync(ex.ToString()).ConfigureAwait(false);
                Environment.Exit(1);
                return;
            }

            Environment.Exit(0);
        }

        public void Dispose()
        {
            CancellationTokenSource?.Dispose();

            foreach (var task in TasksToAwait)
                task.Dispose();
        }
    }
}