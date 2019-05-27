using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TheDialgaTeam.Microsoft.Extensions.DependencyInjection;
using TheDialgaTeam.Xiropht.Xirorig.Services.Console;
using TheDialgaTeam.Xiropht.Xirorig.Services.IO;
using TheDialgaTeam.Xiropht.Xirorig.Services.Pool;

namespace TheDialgaTeam.Xiropht.Xirorig.Services.Setting
{
    public sealed class ConfigService : IInitializable, IDisposable
    {
        public string Host => Config.Pools[0].Host;

        public ushort Port => ushort.TryParse(Config.Pools[0].WalletAddress.Substring(Config.Pools[0].WalletAddress.IndexOf(":", StringComparison.Ordinal) + 1), out var port) ? port : (ushort) 0;

        public string WalletAddress => Config.Pools[0].WalletAddress;

        public Config.MiningThread[] AdditionJobThreads => Config.Threads.Where(a => a.JobType == PoolMiner.JobType.AdditionJob).ToArray();

        public Config.MiningThread[] SubtractionJobThreads => Config.Threads.Where(a => a.JobType == PoolMiner.JobType.SubtractionJob).ToArray();

        public Config.MiningThread[] MultiplicationJobThreads => Config.Threads.Where(a => a.JobType == PoolMiner.JobType.MultiplicationJob).ToArray();

        public Config.MiningThread[] DivisionJobThreads => Config.Threads.Where(a => a.JobType == PoolMiner.JobType.DivisionJob).ToArray();

        public Config.MiningThread[] ModulusJobThreads => Config.Threads.Where(a => a.JobType == PoolMiner.JobType.ModulusJob).ToArray();

        public Config.MiningThread[] RandomJobThreads => Config.Threads.Where(a => a.JobType == PoolMiner.JobType.RandomJob).ToArray();

        private Program Program { get; }

        private FilePathService FilePathService { get; }

        private LoggerService LoggerService { get; }

        private Config Config { get; set; }

        public ConfigService(Program program, FilePathService filePathService, LoggerService loggerService)
        {
            Program = program;
            FilePathService = filePathService;
            LoggerService = loggerService;
        }

        public void Initialize()
        {
            Config = new Config { Threads = new Config.MiningThread[Environment.ProcessorCount / 2] };

            for (var i = 0; i < Config.Threads.Length; i++)
                Config.Threads[i] = new Config.MiningThread { ThreadAffinityToCpu = i * 2 };

            if (!File.Exists(FilePathService.SettingFilePath))
            {
                try
                {
                    using (var streamWriter = new StreamWriter(new FileStream(FilePathService.SettingFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                    {
                        var jsonSerializer = new JsonSerializer { Formatting = Formatting.Indented };
                        jsonSerializer.Serialize(streamWriter, Config);
                    }

                    LoggerService.LogMessage($"Generated Configuration file at: \"{FilePathService.SettingFilePath}\"");
                    LoggerService.LogMessage("Please edit the configuration file before running again :)");
                    LoggerService.LogMessage("Press Enter/Return to continue...");

                    System.Console.ReadLine();
                }
                catch (Exception ex)
                {
                    LoggerService.LogErrorMessage(ex);
                }
                finally
                {
                    Program.CancellationTokenSource.Cancel();
                }
            }
            else
            {
                try
                {
                    using (var streamReader = new StreamReader(new FileStream(FilePathService.SettingFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                    {
                        var jsonSerializer = new JsonSerializer();
                        Config = jsonSerializer.Deserialize<Config>(new JsonTextReader(streamReader));
                    }
                }
                catch (Exception ex)
                {
                    LoggerService.LogErrorMessage(ex);
                    Program.CancellationTokenSource.Cancel();
                }
            }

            var consoleMessages = new ConsoleMessageBuilder()
                .Write(" * ", ConsoleColor.Green)
                .Write("THREADS".PadRight(13))
                .Write(Config.Threads.Length.ToString(), ConsoleColor.Cyan)
                .Write(", ")
                .Write($"donate={Config.DonateLevel}%", Config.DonateLevel > 0 ? ConsoleColor.White : ConsoleColor.Red)
                .WriteLine("", includeDateTime: false);

            for (var i = 0; i < Config.Pools.Length; i++)
            {
                consoleMessages
                    .Write(" * ", ConsoleColor.Green)
                    .Write($"POOL #{i + 1}".PadRight(13))
                    .Write(Config.Pools[i].Host, ConsoleColor.Cyan)
                    .WriteLine("", includeDateTime: false);
            }

            consoleMessages
                .Write(" * ", ConsoleColor.Green)
                .Write("COMMANDS".PadRight(13))
                .Write("h", ConsoleColor.Magenta)
                .Write("ashrate")
                .WriteLine("", includeDateTime: false);

            LoggerService.LogMessage(consoleMessages.Build());
        }

        public void Dispose()
        {
            using (var streamWriter = new StreamWriter(new FileStream(FilePathService.SettingFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
            {
                var jsonSerializer = new JsonSerializer { Formatting = Formatting.Indented };
                jsonSerializer.Serialize(streamWriter, Config);
            }
        }
    }
}