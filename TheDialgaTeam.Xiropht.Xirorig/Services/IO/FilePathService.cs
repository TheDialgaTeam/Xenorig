using System;
using System.IO;
using TheDialgaTeam.Microsoft.Extensions.DependencyInjection;

namespace TheDialgaTeam.Xiropht.Xirorig.Services.IO
{
    public sealed class FilePathService : IInitializable
    {
        public string ConsoleLogFilePath { get; private set; }

        public string SettingFilePath { get; private set; }

        public void Initialize()
        {
            var logsDirectory = Path.Combine(Environment.CurrentDirectory, "Logs");
            var configDirectory = Path.Combine(Environment.CurrentDirectory, "Config");

            if (!Directory.Exists(logsDirectory))
                Directory.CreateDirectory(logsDirectory);

            if (!Directory.Exists(configDirectory))
                Directory.CreateDirectory(configDirectory);

            ConsoleLogFilePath = Path.Combine(logsDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}.txt");
            SettingFilePath = Path.Combine(configDirectory, "Config.json");
        }
    }
}