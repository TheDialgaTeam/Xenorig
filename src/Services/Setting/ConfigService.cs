using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TheDialgaTeam.Microsoft.Extensions.DependencyInjection;
using TheDialgaTeam.Xiropht.Xirorig.Console;
using TheDialgaTeam.Xiropht.Xirorig.Services.Console;
using TheDialgaTeam.Xiropht.Xirorig.Services.IO;
using TheDialgaTeam.Xiropht.Xirorig.Setting;
using Xiropht_Connector_All.Setting;

namespace TheDialgaTeam.Xiropht.Xirorig.Services.Setting
{
    public sealed class ConfigService : IInitializable, IDisposable
    {
        public int DonateLevel => Config.DonateLevel;

        public int PrintTime => Config.PrintTime;

        public Config.MiningMode MiningMode => Config.Mode;

        public Config.MiningSolo Solo => Config.Solo;

        public Config.MiningSoloProxy[] SoloProxies => Config.SoloProxies;

        public Config.MiningPool[] Pools => Config.Pools;

        public Config.MiningThread[] AdditionJobThreads => Config.Threads.Where(a => a.JobType == Config.MiningJob.AdditionJob).ToArray();

        public Config.MiningThread[] SubtractionJobThreads => Config.Threads.Where(a => a.JobType == Config.MiningJob.SubtractionJob).ToArray();

        public Config.MiningThread[] MultiplicationJobThreads => Config.Threads.Where(a => a.JobType == Config.MiningJob.MultiplicationJob).ToArray();

        public Config.MiningThread[] DivisionJobThreads => Config.Threads.Where(a => a.JobType == Config.MiningJob.DivisionJob).ToArray();

        public Config.MiningThread[] ModulusJobThreads => Config.Threads.Where(a => a.JobType == Config.MiningJob.ModulusJob).ToArray();

        public Config.MiningThread[] RandomJobThreads => Config.Threads.Where(a => a.JobType == Config.MiningJob.RandomJob).ToArray();

        private FilePathService FilePathService { get; }

        private LoggerService LoggerService { get; }

        private Config Config { get; set; }

        public ConfigService(FilePathService filePathService, LoggerService loggerService)
        {
            FilePathService = filePathService;
            LoggerService = loggerService;
        }

        public void Initialize()
        {
            var settingFilePath = FilePathService.SettingFilePath;
            var loggerService = LoggerService;

            Config = new Config();

            if (!File.Exists(settingFilePath))
            {
                try
                {
                    // TODO: Write a easy start up manager to configure the miner.

                    using (var streamWriter = new StreamWriter(new FileStream(settingFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                    {
                        var jsonSerializer = new JsonSerializer { Formatting = Formatting.Indented };
                        jsonSerializer.Serialize(streamWriter, Config);
                    }

                    loggerService.LogMessage(new ConsoleMessageBuilder()
                        .WriteLine($"Generated configuration file is at: \"{settingFilePath}\"", false)
                        .WriteLine("Press Enter/Return to exit...")
                        .Build());

                    System.Console.ReadLine();
                }
                finally
                {
                    Program.CancellationTokenSource.Cancel();
                }
            }
            else
            {
                using (var streamReader = new StreamReader(new FileStream(settingFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    var jsonSerializer = new JsonSerializer();
                    Config = jsonSerializer.Deserialize<Config>(new JsonTextReader(streamReader));
                }

                ValidateSettings();
            }

            var config = Config;

            loggerService.LogMessage(new ConsoleMessageBuilder()
                .Write(" * ", ConsoleColor.Green, false)
                .Write("THREADS      ", false)
                .Write(config.Threads.Length.ToString(), ConsoleColor.Cyan, false)
                .Write(", ", false)
                .WriteLine($"donate={config.DonateLevel}%", config.DonateLevel > 0 ? ConsoleColor.White : ConsoleColor.Red, false)
                .Build());
        }

        private void DoStartUpConfiguration()
        {
            LoggerService.LogMessage(new ConsoleMessageBuilder()
                .WriteLine("==================================================", false)
                .WriteLine("Configuration Helper:", false)
                .WriteLine("==================================================", false).Build());

            int mode;

            do
            {
                LoggerService.LogMessage(new ConsoleMessageBuilder()
                    .WriteLine("Please select a mining mode:", false)
                    .WriteLine("1. Solo (Only available on mono build)", false)
                    .WriteLine("2. Solo Proxy", false)
                    .WriteLine("3. Pool", false)
                    .WriteLine("4. Pool Proxy (Reserved option)", false).Build());

                if (int.TryParse(System.Console.In.ReadLine(), out mode) && mode >= 0 && mode <= 4)
                    continue;

                LoggerService.LogMessage(new ConsoleMessageBuilder().WriteLine("Invalid mining mode! Please try again.", ConsoleColor.Red, false).Build());
            } while (mode < 0 || mode > 4);

            Config.Mode = (Config.MiningMode)(mode - 1);

            switch (Config.Mode)
            {
                case Config.MiningMode.Solo:
                    DoConfigureSolo();
                    break;

                case Config.MiningMode.SoloProxy:
                    break;

                case Config.MiningMode.Pool:
                    break;

                case Config.MiningMode.PoolProxy:
                    break;
            }
        }

        private void DoConfigureSolo()
        {
            LoggerService.LogMessage(new ConsoleMessageBuilder()
                .WriteLine("==================================================", false)
                .WriteLine("Solo Mining MiningMode Configuration:", false)
                .WriteLine("==================================================", false).Build());

            string walletAddress;

            do
            {
                LoggerService.LogMessage("Please enter your wallet address:");
                walletAddress = System.Console.In.ReadLine();

                if (string.IsNullOrWhiteSpace(walletAddress) || walletAddress.Length < ClassConnectorSetting.MinWalletAddressSize || walletAddress.Length > ClassConnectorSetting.MaxWalletAddressSize)
                    LoggerService.LogMessage("Invalid wallet address. Please check your wallet address again.", ConsoleColor.Red);
            } while (string.IsNullOrWhiteSpace(walletAddress) || walletAddress.Length < ClassConnectorSetting.MinWalletAddressSize || walletAddress.Length > ClassConnectorSetting.MaxWalletAddressSize);

            Config.Solo = new Config.MiningSolo { WalletAddress = walletAddress };
        }

        private void DoConfigureSoloProxy()
        {
            //LoggerService.LogMessage("==================================================");
            //LoggerService.LogMessage("Solo Proxy Mining MiningMode configuration:");
            //LoggerService.LogMessage("==================================================");

            //string addFailBack;

            //do
            //{
            //    string host;
            //    ushort port;
            //    string workerId;

            //    do
            //    {
            //        LoggerService.LogMessage("Please enter your solo proxy address (127.0.0.1 for local):");
            //        host = System.Console.In.ReadLine();

            //        if (string.IsNullOrWhiteSpace(host))
            //            LoggerService.LogMessage("Invalid address. Please try again.", ConsoleColor.Red);
            //    } while (string.IsNullOrWhiteSpace(host));

            //    LoggerService.LogMessage("Do you wish to add a fail back solo proxy? [Y/N]:");
            //} while (addFailBack != null && addFailBack.Equals("y", StringComparison.OrdinalIgnoreCase));
        }

        private void ValidateSettings()
        {
            var config = Config;

            if (config.DonateLevel < 1)
                config.DonateLevel = 1;

            if (config.DonateLevel > 100)
                config.DonateLevel = 100;

            if (config.PrintTime < 1)
                config.PrintTime = 1;

            if (config.Safe && config.Threads.Length > Environment.ProcessorCount)
                throw new ArgumentException("Excessive amount of thread allocated which may cause unstable results. Use \"Safe: false\" if you intend to use this configuration.");

            switch (config.Mode)
            {
                case Config.MiningMode.Solo:
                    if (config.Solo.WalletAddress.Length < ClassConnectorSetting.MinWalletAddressSize || config.Solo.WalletAddress.Length > ClassConnectorSetting.MaxWalletAddressSize)
                        throw new ArgumentException("Invalid Xiropht wallet address. Please check your wallet address again.");
                    break;

                case Config.MiningMode.SoloProxy:
                    foreach (var soloProxy in config.SoloProxies)
                    {
                        if (soloProxy.Host.Contains(":"))
                            throw new ArgumentException("Invalid solo proxy host. Please remove the port from the host.");

                        if (string.IsNullOrWhiteSpace(soloProxy.WorkerId))
                            throw new ArgumentException("Worker Id should not be blank.");
                    }
                    break;

                case Config.MiningMode.Pool:
                    foreach (var pool in config.Pools)
                    {
                        if (pool.Host.Contains(":"))
                            throw new ArgumentException("Invalid pool host. Please remove the port from the host.");

                        if (pool.WalletAddress.Length < ClassConnectorSetting.MinWalletAddressSize)
                            throw new ArgumentException("Invalid Xiropht wallet address. Please check your wallet address again.");
                    }
                    break;

                case Config.MiningMode.PoolProxy:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
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