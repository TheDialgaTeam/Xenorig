using System;
using System.Reflection;
using System.Runtime.Versioning;
using TheDialgaTeam.Microsoft.Extensions.DependencyInjection;
using TheDialgaTeam.Xiropht.Xirorig.Services.Console;

namespace TheDialgaTeam.Xiropht.Xirorig.Services.Bootstrap
{
    public sealed class BootstrapService : IInitializable
    {
        private LoggerService LoggerService { get; }

        public BootstrapService(LoggerService loggerService)
        {
            LoggerService = loggerService;
        }

        public void Initialize()
        {
            var title = $"Xirorig (.Net Core) v{Assembly.GetExecutingAssembly().GetName().Version}";
            System.Console.Title = title;

            var consoleMessages = new ConsoleMessageBuilder()
                .Write(" * ", ConsoleColor.Green)
                .Write("ABOUT".PadRight(13))
                .Write($"Xirorig/{Assembly.GetExecutingAssembly().GetName().Version} ", ConsoleColor.Cyan)
                .Write(Assembly.GetExecutingAssembly().GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName)
                .WriteLine("", includeDateTime: false)
                .Build();

            LoggerService.LogMessage(consoleMessages);
        }
    }
}