using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using TheDialgaTeam.Core.Logger;
using TheDialgaTeam.Core.Logger.Formatter;
using TheDialgaTeam.Xiropht.Xirorig.Config;
using TheDialgaTeam.Xiropht.Xirorig.Miner;
using TheDialgaTeam.Xiropht.Xirorig.Network;

namespace TheDialgaTeam.Xiropht.Xirorig
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).RunConsoleAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostBuilderContext, serviceCollection) =>
                {
                    serviceCollection.AddSingleton<XirorigConfiguration>();
                    serviceCollection.AddSingleton<XirorigToSeedNetwork>();

                    serviceCollection.AddHostedService<ProgramHostedService>();
                    serviceCollection.AddHostedService<MinerHostedService>();
                })
                .UseSerilog((hostBuilderContext, serviceProvider, loggerConfiguration) =>
                {
                    const string outputTemplate = "{Message}{NewLine}{Exception}";

                    loggerConfiguration
                        .ReadFrom.Configuration(hostBuilderContext.Configuration)
                        .WriteTo.AnsiConsole(new OutputTemplateTextFormatter(outputTemplate));
                });
            ;
        }
    }
}