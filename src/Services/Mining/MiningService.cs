using TheDialgaTeam.Microsoft.Extensions.DependencyInjection;
using TheDialgaTeam.Xiropht.Xirorig.Services.Console;
using TheDialgaTeam.Xiropht.Xirorig.Services.Setting;

namespace TheDialgaTeam.Xiropht.Xirorig.Services.Mining
{
    public sealed class MiningService : IInitializable
    {
        private Program Program { get; }

        private LoggerService LoggerService { get; }

        private ConfigService ConfigService { get; }

        public MiningService(Program program, LoggerService loggerService, ConfigService configService)
        {
            Program = program;
            LoggerService = loggerService;
            ConfigService = configService;
        }

        public void Initialize()
        {
            
        }
    }
}