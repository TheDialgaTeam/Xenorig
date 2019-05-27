using System.Reflection;
using TheDialgaTeam.Microsoft.Extensions.DependencyInjection;
using TheDialgaTeam.Xiropht.Xirorig.Core.Services.Console;

namespace TheDialgaTeam.Xiropht.Xirorig.Core.Services.Bootstrap
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

            LoggerService.LogMessage("==================================================");
            LoggerService.LogMessage(title);
            LoggerService.LogMessage("==================================================");
        }
    }
}