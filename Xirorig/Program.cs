using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using TheDialgaTeam.Core.Logger.Extensions.Logging;
using TheDialgaTeam.Core.Logger.Serilog.Formatting.Ansi;
using TheDialgaTeam.Core.Logger.Serilog.Sinks;
using Xirorig.Options;

namespace Xirorig
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            try
            {
                await CreateHostBuilder(args).RunConsoleAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostBuilderContext, serviceCollection) =>
                {
                    serviceCollection.Configure<XirorigOptions>(hostBuilderContext.Configuration.GetSection("Xirorig"));

                    // Logger
                    serviceCollection.AddLoggingTemplate(new LoggerTemplateConfiguration(configuration =>
                    {
                        configuration.Global.DefaultPrefixTemplate = $"{AnsiEscapeCodeConstants.DarkGrayForegroundColor}{{DateTimeOffset:yyyy-MM-dd HH:mm:ss}}{AnsiEscapeCodeConstants.Reset} ";
                        configuration.Global.DefaultPrefixTemplateArgs = () => new object[] { DateTimeOffset.Now };
                    }));

                    // Application Context
                    serviceCollection.AddSingleton<ApplicationContext>();

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