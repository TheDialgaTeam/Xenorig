using System;
using System.IO;
using TheDialgaTeam.Microsoft.Extensions.DependencyInjection;

namespace TheDialgaTeam.Xiropht.Xirorig.Services.IO
{
    public sealed class FilePathService : IInitializable
    {
        public string SettingFilePath { get; private set; }

        public void Initialize()
        {
            var configDirectory = Path.Combine(Environment.CurrentDirectory, "Config");

            if (!Directory.Exists(configDirectory))
                Directory.CreateDirectory(configDirectory);

            SettingFilePath = Path.Combine(configDirectory, "Config.json");
        }
    }
}