#pragma warning disable IL2026

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TheDialgaTeam.Core.Logging.Microsoft;
using Xenorig.Options;
using Xenorig.Utilities;

namespace Xenorig;

public static class Program
{
    public const string XenoNativeLibrary = "xeno_native";

    public static Task Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainOnUnhandledException;

        return Host.CreateDefaultBuilder(args)
            .ConfigureServices(collection =>
            {
                collection.AddOptions<XenorigOptions>().BindConfiguration("Xenorig", options => options.BindNonPublicProperties = true);
                collection.AddHostedService<ConsoleService>();
            })
            .ConfigureLogging(builder =>
            {
                builder.AddLoggerTemplateFormatter(options =>
                {
                    options.SetDefaultTemplate(formattingBuilder => formattingBuilder.SetGlobal(messageFormattingBuilder => messageFormattingBuilder.SetPrefix((in LoggerTemplateEntry _) => $"{AnsiEscapeCodeConstants.DarkGrayForegroundColor}{DateTime.Now:yyyy-MM-dd HH:mm:ss}{AnsiEscapeCodeConstants.Reset} ")));
                    options.SetTemplate<ConsoleService>(formattingBuilder => formattingBuilder.SetGlobal(messageFormattingBuilder => messageFormattingBuilder.SetPrefix(string.Empty)));
                });
            })
            .RunConsoleAsync();
    }

    private static void OnCurrentDomainOnUnhandledException(object _, UnhandledExceptionEventArgs eventArgs)
    {
        if (!eventArgs.IsTerminating) return;

        var crashFileLocation = Path.Combine(AppContext.BaseDirectory, $"{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_crash.log");
        File.WriteAllText(crashFileLocation, eventArgs.ExceptionObject.ToString());
    }
}