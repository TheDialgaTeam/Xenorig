using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TheDialgaTeam.Serilog.Extensions;
using TheDialgaTeam.Serilog.Formatting;
using TheDialgaTeam.Serilog.Sinks.AnsiConsole;
using Xenorig.Options;

namespace Xenorig;

internal static class Program
{
    public static Task Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainOnUnhandledException;

        return Host.CreateDefaultBuilder(args)
            .ConfigureServices(collection =>
            {
                collection.AddOptions<XenorigOptions>().BindConfiguration("Xenorig");
                collection.AddHostedService<ConsoleService>();
            })
            .ConfigureSerilog((context, provider, configuration) =>
            {
                configuration.WriteTo.AnsiConsoleSink(builder => builder
                    .SetDefault(templateBuilder => templateBuilder.SetDefault($"{AnsiEscapeCodeConstants.DarkGrayForegroundColor}{{Timestamp:yyyy-MM-dd HH:mm:ss}}{AnsiEscapeCodeConstants.Reset} {{Message:l}}{{NewLine}}{{Exception}}"))
                    .SetOverrides("Xenorig.ConsoleService", templateBuilder => templateBuilder.SetDefault("{Message:l}{NewLine}{Exception}"))
                );
            })
            .RunConsoleAsync(options => options.SuppressStatusMessages = true);
    }

    private static void OnCurrentDomainOnUnhandledException(object _, UnhandledExceptionEventArgs eventArgs)
    {
        if (!eventArgs.IsTerminating) return;

        var crashFileLocation = Path.Combine(AppContext.BaseDirectory, $"{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_crash.log");
        File.WriteAllText(crashFileLocation, eventArgs.ExceptionObject.ToString());
    }
}