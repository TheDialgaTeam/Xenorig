using System;
using System.Reflection;
using System.Runtime.Versioning;
using TheDialgaTeam.Microsoft.Extensions.DependencyInjection;
using TheDialgaTeam.Xiropht.Xirorig.Console;
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
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var frameworkVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName;
            System.Console.Title = $"Xirorig v{version} ({frameworkVersion})";

            LoggerService.LogMessage(new ConsoleMessageBuilder()
                .Write(" * ", ConsoleColor.Green, false)
                .Write("ABOUT        ", false)
                .Write($"Xirorig/{version} ", ConsoleColor.Cyan, false)
                .WriteLine(frameworkVersion, false)
                .Build());
        }
    }
}