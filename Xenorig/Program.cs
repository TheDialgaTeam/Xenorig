using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TheDialgaTeam.Core.Logging.Microsoft;
using Xenorig.Options;

namespace Xenorig;

internal static class Program
{
    private static class Native
    {
        public const int StandardOutputHandleId = -11;
        public const int EnableVirtualTerminalProcessingMode = 4;
        public const long InvalidHandleValue = -1;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetStdHandle(int handleId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetConsoleMode(IntPtr handle, out int mode);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleMode(IntPtr handle, int mode);
    }

    public const string XenoNativeLibrary = "xeno_native";

    [SuppressMessage("Dependent Types", "IL2026")]
    public static async Task Main(string[] args)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var stdout = Native.GetStdHandle(Native.StandardOutputHandleId);

            if (stdout != (IntPtr) Native.InvalidHandleValue && Native.GetConsoleMode(stdout, out var mode))
            {
                Native.SetConsoleMode(stdout, mode | Native.EnableVirtualTerminalProcessingMode);
            }
        }

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, collection) =>
            {
                collection.Configure<XenorigOptions>(context.Configuration.GetSection("Xenorig"));
                collection.AddHostedService<ProgramHostedService>();
            })
            .ConfigureLogging(builder =>
            {
                builder.AddLoggerTemplateFormatter(options =>
                {
                    options.SetDefaultTemplate(template => template.Global.SetPrefix((in LoggerTemplateEntry _) => $"{AnsiEscapeCodeConstants.GrayForegroundColor}{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}{AnsiEscapeCodeConstants.Reset} "));
                    options.SetTemplate<ProgramHostedService>();
                });
            })
            .UseConsoleLifetime()
            .Build();

        var contentRootPath = host.Services.GetService<IHostEnvironment>()?.ContentRootPath ?? Environment.CurrentDirectory;

        try
        {
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            var crashFileLocation = Path.Combine(contentRootPath, $"{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_crash.log");
            await File.WriteAllTextAsync(crashFileLocation, ex.ToString());
        }
    }
}