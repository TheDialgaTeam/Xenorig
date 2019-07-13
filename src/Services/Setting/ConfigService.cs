using System;
using System.IO;
using System.Linq;
using System.Threading;
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
                    DoStartUpConfiguration();

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
            var loggerService = LoggerService;

            loggerService.LogMessage(new ConsoleMessageBuilder()
                .WriteLine("==================================================", false)
                .WriteLine("Configuration Setup:", false)
                .WriteLine("==================================================", false)
                .Build());

            int mode;

            do
            {
                loggerService.LogMessage(new ConsoleMessageBuilder()
                    .WriteLine("Please select a mining mode:", false)
                    .WriteLine("1. Solo (Only available on mono build)", false)
                    .WriteLine("2. Solo Proxy (Not implemented)", false)
                    .WriteLine("3. Pool", false)
                    .WriteLine("4. Pool Proxy (Not implemented)", false)
                    .Build());

                if (int.TryParse(System.Console.In.ReadLine(), out mode) && mode >= 1 && mode <= 4)
                    continue;

                loggerService.LogMessage("Invalid mining mode! Please try again.", ConsoleColor.Red, false);
            } while (mode < 0 || mode > 4);

            Config.Mode = (Config.MiningMode) (mode - 1);

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
            var loggerService = LoggerService;

            loggerService.LogMessage(new ConsoleMessageBuilder()
                .WriteLine("==================================================", false)
                .WriteLine("Solo Mining Mode Configuration:", false)
                .WriteLine("==================================================", false)
                .Build());

            string walletAddress;

            do
            {
                loggerService.LogMessage("Please enter your wallet address:", false);
                walletAddress = System.Console.In.ReadLine();

                if (string.IsNullOrWhiteSpace(walletAddress) || walletAddress.Length < ClassConnectorSetting.MinWalletAddressSize || walletAddress.Length > ClassConnectorSetting.MaxWalletAddressSize)
                    loggerService.LogMessage("Invalid wallet address. Please check your wallet address again.", ConsoleColor.Red, false);
            } while (string.IsNullOrWhiteSpace(walletAddress) || walletAddress.Length < ClassConnectorSetting.MinWalletAddressSize || walletAddress.Length > ClassConnectorSetting.MaxWalletAddressSize);

            Config.Solo = new Config.MiningSolo { WalletAddress = walletAddress };

            DoConfigureThread();
        }

        private void DoConfigureThread()
        {
            var loggerService = LoggerService;

            loggerService.LogMessage(new ConsoleMessageBuilder()
                .WriteLine("==================================================", false)
                .WriteLine("Thread Configuration:", false)
                .WriteLine("==================================================", false)
                .Build());

            int numberOfThreads;

            do
            {
                loggerService.LogMessage(new ConsoleMessageBuilder()
                    .WriteLine($"Your CPU have {Environment.ProcessorCount} processors.", false)
                    .WriteLine("Please select the number of threads to mine:", false)
                    .Build());

                if (int.TryParse(System.Console.In.ReadLine(), out numberOfThreads) && numberOfThreads > 0 && numberOfThreads <= Environment.ProcessorCount)
                    continue;

                loggerService.LogMessage("You have selected invalid number of threads.", ConsoleColor.Red, false);
            } while (numberOfThreads < 0 || numberOfThreads > Environment.ProcessorCount);

            var miningThreads = new Config.MiningThread[numberOfThreads];

            for (var i = 0; i < numberOfThreads; i++)
            {
                var miningThread = new Config.MiningThread();

                loggerService.LogMessage(new ConsoleMessageBuilder()
                    .WriteLine("==================================================", false)
                    .WriteLine($"Thread #{i + 1} Configuration:", false)
                    .WriteLine("==================================================", false)
                    .Build());

                int jobType;

                do
                {
                    loggerService.LogMessage(new ConsoleMessageBuilder()
                        .WriteLine("Please select mining job type:", false)
                        .WriteLine("1. Random Job", false)
                        .WriteLine("2. Addition Job (Not implemented)", false)
                        .WriteLine("3. Subtraction Job (Not implemented)", false)
                        .WriteLine("4. Multiplication Job (Not implemented)", false)
                        .WriteLine("5. Division Job (Not implemented)", false)
                        .WriteLine("6. Modulus Job (Not implemented)", false)
                        .Build());

                    if (int.TryParse(System.Console.In.ReadLine(), out jobType) && jobType >= 1 && jobType <= 6)
                        continue;

                    loggerService.LogMessage("Invalid mining job type! Please try again.", ConsoleColor.Red, false);
                } while (jobType < 0 || jobType > 6);

                miningThread.JobType = (Config.MiningJob) (jobType - 1);

                int threadPriority;

                do
                {
                    loggerService.LogMessage(new ConsoleMessageBuilder()
                        .WriteLine("Please select thread priority:", false)
                        .WriteLine("1. Lowest", false)
                        .WriteLine("2. Below Normal", false)
                        .WriteLine("3. Normal", false)
                        .WriteLine("4. Above Normal", false)
                        .WriteLine("5. Highest", false)
                        .Build());

                    if (int.TryParse(System.Console.In.ReadLine(), out threadPriority) && threadPriority >= 1 && threadPriority <= 5)
                        continue;

                    loggerService.LogMessage("Invalid thread priority! Please try again.", ConsoleColor.Red, false);
                } while (threadPriority < 0 || threadPriority > 5);

                miningThread.ThreadPriority = (ThreadPriority) (threadPriority - 1);

                int miningPriority;

                do
                {
                    loggerService.LogMessage(new ConsoleMessageBuilder()
                        .WriteLine("Please select mining priority:", false)
                        .WriteLine("1. Prefer shares over block.", false)
                        .WriteLine("2. Normal Behavior", false)
                        .WriteLine("3. Prefer block over shares.", false)
                        .WriteLine("Note: In Solo mining mode, this does not matter.", false)
                        .Build());

                    if (int.TryParse(System.Console.In.ReadLine(), out miningPriority) && miningPriority >= 1 && miningPriority <= 3)
                        continue;

                    loggerService.LogMessage("Invalid thread priority! Please try again.", ConsoleColor.Red, false);
                } while (miningPriority < 0 || miningPriority > 3);

                miningThread.MiningPriority = (Config.MiningPriority) (miningPriority - 1);

                string shareRange;

                do
                {
                    loggerService.LogMessage("Do you wish to split the job range evenly with other threads? [Y/N]:", false);
                    shareRange = System.Console.In.ReadLine();

                    if (!string.IsNullOrWhiteSpace(shareRange) && (shareRange.Equals("y", StringComparison.OrdinalIgnoreCase) || shareRange.Equals("n", StringComparison.OrdinalIgnoreCase)))
                        continue;

                    loggerService.LogMessage("Invalid option. Please try again.", ConsoleColor.Red, false);
                } while (string.IsNullOrWhiteSpace(shareRange) || !shareRange.Equals("y", StringComparison.OrdinalIgnoreCase) && !shareRange.Equals("n", StringComparison.OrdinalIgnoreCase));

                miningThread.ShareRange = shareRange.Equals("y", StringComparison.OrdinalIgnoreCase);

                if (miningThread.ShareRange)
                {
                    miningThreads[i] = miningThread;
                    continue;
                }

                string customRange;

                do
                {
                    loggerService.LogMessage("Do you wish to have a custom mining range? [Y/N]:", false);
                    customRange = System.Console.In.ReadLine();

                    if (!string.IsNullOrWhiteSpace(customRange) && (customRange.Equals("y", StringComparison.OrdinalIgnoreCase) || customRange.Equals("n", StringComparison.OrdinalIgnoreCase)))
                        continue;

                    loggerService.LogMessage("Invalid option. Please try again.", ConsoleColor.Red, false);
                } while (string.IsNullOrWhiteSpace(customRange) || !customRange.Equals("y", StringComparison.OrdinalIgnoreCase) && !customRange.Equals("n", StringComparison.OrdinalIgnoreCase));

                if (shareRange.Equals("n", StringComparison.OrdinalIgnoreCase))
                {
                    miningThreads[i] = miningThread;
                    continue;
                }

                int minMiningRange;

                do
                {
                    loggerService.LogMessage("Please enter the minimum mining range in percentage [Min: 0, Max: 99]", false);

                    if (int.TryParse(System.Console.In.ReadLine(), out minMiningRange) && minMiningRange >= 0 && minMiningRange <= 99)
                        continue;

                    loggerService.LogMessage("Invalid mining range! Please try again.", ConsoleColor.Red, false);
                } while (minMiningRange < 0 || minMiningRange > 99);

                miningThread.MinMiningRangePercentage = minMiningRange;

                int maxMiningRange;

                do
                {
                    loggerService.LogMessage($"Please enter the maximum mining range in percentage [Min: {minMiningRange + 1}, Max: 100]", false);

                    if (int.TryParse(System.Console.In.ReadLine(), out maxMiningRange) && maxMiningRange >= minMiningRange + 1 && maxMiningRange <= 100)
                        continue;

                    loggerService.LogMessage("Invalid mining range! Please try again.", ConsoleColor.Red, false);
                } while (maxMiningRange < minMiningRange + 1 || maxMiningRange > 100);

                miningThread.MaxMiningRangePercentage = maxMiningRange;
                miningThreads[i] = miningThread;
            }

            Config.Threads = miningThreads;
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