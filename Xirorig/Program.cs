using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using TheDialgaTeam.Extensions.Logging.LoggingTemplate;
using TheDialgaTeam.Serilog.Formatting.Ansi;
using TheDialgaTeam.Serilog.Sinks.AnsiConsole;
using Xirorig.Algorithm;
using Xirorig.Network;
using Xirorig.Options;

namespace Xirorig
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).RunConsoleAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostBuilderContext, serviceCollection) =>
                {
                    serviceCollection.Configure<XirorigOptions>(hostBuilderContext.Configuration.GetSection("Xirorig"));

                    // Logger
                    serviceCollection.AddLoggingTemplate();

                    // Algorithm
                    serviceCollection.AddSingleton<IAlgorithm, XirophtAlgorithm>();
                    serviceCollection.AddSingleton<IAlgorithm, XirobodAlgorithm>();

                    // Network
                    serviceCollection.AddSingleton<XirorigNetwork>();

                    // Program
                    serviceCollection.AddHostedService<ProgramHostedService>();
                })
                .UseSerilog((hostBuilderContext, _, loggerConfiguration) =>
                {
                    loggerConfiguration
                        .ReadFrom.Configuration(hostBuilderContext.Configuration)
                        .WriteTo.AnsiConsole(new AnsiOutputTemplateTextFormatter("{Message}{NewLine}{Exception}"));
                });
        }
    }
}