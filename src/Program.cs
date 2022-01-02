using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using TheDialgaTeam.Core.Logger.Serilog.Formatting.Ansi;
using TheDialgaTeam.Core.Logger.Serilog.Sinks;
using TheDialgaTeam.Xiropht.Xirorig.Config;
using TheDialgaTeam.Xiropht.Xirorig.Miner;
using TheDialgaTeam.Xiropht.Xirorig.Network;

namespace TheDialgaTeam.Xiropht.Xirorig
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                await CreateHostBuilder(args).RunConsoleAsync();
            }
            catch (Exception ex)
            {
#if DEBUG
                throw;
#endif
                var errorLogDirectory = Path.Combine(Environment.CurrentDirectory, "errors");

                if (!Directory.Exists(errorLogDirectory))
                {
                    Directory.CreateDirectory(errorLogDirectory);
                }

                await using var fileStream = new FileStream(Path.Combine(errorLogDirectory, $"{DateTimeOffset.Now:yyyy-MM-dd-HH-mm-ss}.log"), FileMode.Append, FileAccess.Write, FileShare.Read);
                await using var writer = new StreamWriter(fileStream, Encoding.UTF8);

                await writer.WriteLineAsync(ex.ToString()).ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((_, serviceCollection) =>
                {
                    serviceCollection.AddSingleton<XirorigConfiguration>();
                    serviceCollection.AddSingleton<XirorigToSeedNetwork>();

                    serviceCollection.AddHostedService<ProgramHostedService>();
                    serviceCollection.AddHostedService<MinerHostedService>();
                })
                .UseSerilog((hostBuilderContext, _, loggerConfiguration) =>
                {
                    const string outputTemplate = "{Message}{NewLine}{Exception}";

                    loggerConfiguration
                        .ReadFrom.Configuration(hostBuilderContext.Configuration)
                        .WriteTo.AnsiConsole(new AnsiOutputTemplateTextFormatter(outputTemplate));
                });
            ;
        }
    }
}