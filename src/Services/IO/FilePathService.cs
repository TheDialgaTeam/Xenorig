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
            var currentDirectory = Environment.CurrentDirectory;
            var logsDirectory = Path.Combine(currentDirectory, "Logs");
            var configDirectory = Path.Combine(currentDirectory, "Config");

            if (!Directory.Exists(logsDirectory))
                Directory.CreateDirectory(logsDirectory);

            if (!Directory.Exists(configDirectory))
                Directory.CreateDirectory(configDirectory);

            ConsoleLogFilePath = Path.Combine(logsDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}.txt");
            SettingFilePath = Path.Combine(configDirectory, "Config.json");
        }
    }
}