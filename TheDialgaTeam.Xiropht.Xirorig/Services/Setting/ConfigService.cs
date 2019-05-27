using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TheDialgaTeam.Microsoft.Extensions.DependencyInjection;
using TheDialgaTeam.Xiropht.Xirorig.Services.Console;
using TheDialgaTeam.Xiropht.Xirorig.Services.IO;
using TheDialgaTeam.Xiropht.Xirorig.Services.Pool;
using Xiropht_Connector_All.Setting;

namespace TheDialgaTeam.Xiropht.Xirorig.Services.Setting
{
    public sealed class ConfigService : IInitializable, IDisposable
    {
        public string Host => Config.Host;

        public ushort Port => Config.Port;

        public string WalletAddress => Config.WalletAddress;

        public Config.MiningThread[] AdditionJobThreads => Config.MiningThreads.Where(a => a.JobType == PoolMiner.JobType.AdditionJob).ToArray();

        public Config.MiningThread[] SubtractionJobThreads => Config.MiningThreads.Where(a => a.JobType == PoolMiner.JobType.SubtractionJob).ToArray();

        public Config.MiningThread[] MultiplicationJobThreads => Config.MiningThreads.Where(a => a.JobType == PoolMiner.JobType.MultiplicationJob).ToArray();

        public Config.MiningThread[] DivisionJobThreads => Config.MiningThreads.Where(a => a.JobType == PoolMiner.JobType.DivisionJob).ToArray();

        public Config.MiningThread[] ModulusJobThreads => Config.MiningThreads.Where(a => a.JobType == PoolMiner.JobType.ModulusJob).ToArray();

        public Config.MiningThread[] RandomJobThreads => Config.MiningThreads.Where(a => a.JobType == PoolMiner.JobType.RandomJob).ToArray();

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
            Config = new Config { MiningThreads = new Config.MiningThread[Environment.ProcessorCount / 2] };

            for (var i = 0; i < Config.MiningThreads.Length; i++)
                Config.MiningThreads[i] = new Config.MiningThread { ThreadAffinityToCpu = i * 2 };

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

                    if (string.IsNullOrWhiteSpace(Host) || Port == 0)
                        throw new ArgumentException("Invalid host or port to connect.");

                    if (string.IsNullOrWhiteSpace(WalletAddress) || WalletAddress.Length < ClassConnectorSetting.MinWalletAddressSize || WalletAddress.Length > ClassConnectorSetting.MaxWalletAddressSize)
                        throw new ArgumentException("Invalid Xiropht wallet address.");

                    foreach (var configMiningThread in Config.MiningThreads)
                    {
                        if (configMiningThread.ThreadPriority < 0 || configMiningThread.ThreadPriority > 4)
                            throw new ArgumentException("Invalid thread priority.");

                        if (configMiningThread.ThreadAffinityToCpu < 0)
                            throw new ArgumentException("Invalid thread affinity. Set 0 for any cpu.");
                    }

                    LoggerService.LogMessage("Config loaded!");
                }
                catch (Exception ex)
                {
                    LoggerService.LogErrorMessage(ex);
                    Program.CancellationTokenSource.Cancel();
                }
            }
        }

        public void Dispose()
        {
            using (var streamWriter = new StreamWriter(new FileStream(FilePathService.SettingFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
            {
                var jsonSerializer = new JsonSerializer { Formatting = Formatting.Indented };
                jsonSerializer.Serialize(streamWriter, Config);
            }

            LoggerService.LogMessage($"Saving Configuration file at: {FilePathService.SettingFilePath}");
        }
    }
}