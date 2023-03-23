using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TheDialgaTeam.Core.Logging.Microsoft;
using TheDialgaTeam.Xiropht.Xirorig.Config;
using TheDialgaTeam.Xiropht.Xirorig.Miner;
using TheDialgaTeam.Xiropht.Xirorig.Network;

namespace TheDialgaTeam.Xiropht.Xirorig;

public static class Program
{
    public static async Task Main(string[] args)
    {
        await Host.CreateDefaultBuilder(args)
            .ConfigureServices((_, serviceCollection) =>
            {
                serviceCollection.AddSingleton<XirorigConfiguration>();
                serviceCollection.AddSingleton<XirorigToSeedNetwork>();
                
                serviceCollection.AddHostedService<MinerService>();
            })
            .ConfigureLogging(builder =>
            {
                builder.AddLoggerTemplateFormatter(options =>
                {
                    options.SetDefaultTemplate(formattingBuilder => formattingBuilder.SetGlobal(messageFormattingBuilder => messageFormattingBuilder.SetPrefix($"{AnsiEscapeCodeConstants.DarkGrayForegroundColor}{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}{AnsiEscapeCodeConstants.Reset} ")));
                    options.SetTemplate<MinerService>(formattingBuilder => formattingBuilder.SetGlobal(messageFormattingBuilder => messageFormattingBuilder.SetPrefix(string.Empty)));
                });
            }).RunConsoleAsync();

        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainOnUnhandledException;
    }

    private static void OnCurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (!e.IsTerminating) return;
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, $"{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_crash.log"), e.ExceptionObject.ToString());
    }
}