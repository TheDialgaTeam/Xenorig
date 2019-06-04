using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TheDialgaTeam.Microsoft.Extensions.DependencyInjection;
using TheDialgaTeam.Xiropht.Xirorig.Services.Console;
using TheDialgaTeam.Xiropht.Xirorig.Services.IO;
using Xiropht_Connector_All.Setting;

namespace TheDialgaTeam.Xiropht.Xirorig.Services.Setting
{
    public sealed class ConfigService : IInitializable, IDisposable
    {
        public int DonateLevel => Config.DonateLevel;

        public int PrintTime => Config.PrintTime;

        public Config.MiningPool[] Pools => Config.Pools;

        public Config.MiningThread[] AdditionJobThreads => Config.Threads.Where(a => a.JobType == Config.MiningJob.AdditionJob).ToArray();

        public Config.MiningThread[] SubtractionJobThreads => Config.Threads.Where(a => a.JobType == Config.MiningJob.SubtractionJob).ToArray();

        public Config.MiningThread[] MultiplicationJobThreads => Config.Threads.Where(a => a.JobType == Config.MiningJob.MultiplicationJob).ToArray();

        public Config.MiningThread[] DivisionJobThreads => Config.Threads.Where(a => a.JobType == Config.MiningJob.DivisionJob).ToArray();

        public Config.MiningThread[] ModulusJobThreads => Config.Threads.Where(a => a.JobType == Config.MiningJob.ModulusJob).ToArray();

        public Config.MiningThread[] RandomJobThreads => Config.Threads.Where(a => a.JobType == Config.MiningJob.RandomJob).ToArray();

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
            Config = new Config();

            if (!File.Exists(FilePathService.SettingFilePath))
            {
                try
                {
                    using (var streamWriter = new StreamWriter(new FileStream(FilePathService.SettingFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                    {
                        var jsonSerializer = new JsonSerializer { Formatting = Formatting.Indented };
                        jsonSerializer.Serialize(streamWriter, Config);
                    }

                    LoggerService.LogMessage(new ConsoleMessageBuilder().WriteLine("Running configuration setup...", includeDateTime: false).Build());

                    var walletAddress = "";
                    var host = "";
                    ushort port = 0;
                    var threads = 0;

                    do
                    {
                        LoggerService.LogMessage(new ConsoleMessageBuilder().WriteLine("Write your wallet address (Append a period with the difficulty number if you wish to use static difficulty):", includeDateTime: false).Build());
                        walletAddress = System.Console.ReadLine();

                        if (string.IsNullOrWhiteSpace(walletAddress) || walletAddress.Length < ClassConnectorSetting.MinWalletAddressSize)
                            LoggerService.LogMessage(new ConsoleMessageBuilder().WriteLine("Input wallet address is wrong, Xiropht wallet addresses are between 48 and 96 characters long.", ConsoleColor.Red, false).Build());
                    } while (string.IsNullOrWhiteSpace(walletAddress) || walletAddress.Length < ClassConnectorSetting.MinWalletAddressSize);

                    do
                    {
                        LoggerService.LogMessage(new ConsoleMessageBuilder().WriteLine("Write the mining pool host:", includeDateTime: false).Build());
                        host = System.Console.ReadLine();

                        if (string.IsNullOrWhiteSpace(host))
                            LoggerService.LogMessage(new ConsoleMessageBuilder().WriteLine("Invalid host.", ConsoleColor.Red, false).Build());
                    } while (string.IsNullOrWhiteSpace(host));

                    do
                    {
                        LoggerService.LogMessage(new ConsoleMessageBuilder().WriteLine("Write the mining pool port:", includeDateTime: false).Build());

                        if (!ushort.TryParse(System.Console.ReadLine(), out port))
                            LoggerService.LogMessage(new ConsoleMessageBuilder().WriteLine("Invalid port.", ConsoleColor.Red, false).Build());
                        else if (port == 0)
                            LoggerService.LogMessage(new ConsoleMessageBuilder().WriteLine("Invalid port.", ConsoleColor.Red, false).Build());
                    } while (port == 0);

                    do
                    {
                        LoggerService.LogMessage(new ConsoleMessageBuilder().WriteLine($"Select the number of thread to use, detected thread {Environment.ProcessorCount}:", includeDateTime: false).Build());

                        if (!int.TryParse(System.Console.ReadLine(), out threads))
                            LoggerService.LogMessage(new ConsoleMessageBuilder().WriteLine("Invalid thread count.", ConsoleColor.Red, false).Build());
                        else if (threads <= 0)
                            LoggerService.LogMessage(new ConsoleMessageBuilder().WriteLine("Invalid thread count.", ConsoleColor.Red, false).Build());
                    } while (threads <= 0);

                    Config.Pools = new[] { new Config.MiningPool { Host = host, Port = port, WalletAddress = walletAddress } };
                    Config.Threads = new Config.MiningThread[threads];

                    for (var i = 0; i < Config.Threads.Length; i++)
                        Config.Threads[i] = new Config.MiningThread();

                    LoggerService.LogMessage($"Generated Configuration file at: \"{FilePathService.SettingFilePath}\"");
                    LoggerService.LogMessage("You may want to edit the configuration file for advanced settings :)");
                    LoggerService.LogMessage("Press Enter/Return to exit...");

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

                    ValidateSettings();
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
                    .Write($"{Config.Pools[i].Host}:{Config.Pools[i].Port}", ConsoleColor.Cyan)
                    .WriteLine("", includeDateTime: false);
            }

            LoggerService.LogMessage(consoleMessages.Build());
        }

        private void ValidateSettings()
        {
            try
            {
                if (DonateLevel < 1)
                    Config.DonateLevel = 1;

                if (DonateLevel > 100)
                    Config.DonateLevel = 100;

                if (PrintTime < 1)
                    Config.PrintTime = 1;

                if (Config.Safe && Config.Threads.Length > Environment.ProcessorCount)
                    throw new ArgumentOutOfRangeException(nameof(Config.Threads), "Excessive amount of thread allocated which may cause unstable results. Use \"Safe: false\" if you intend to use this configuration.");

                foreach (var miningPool in Pools)
                {
                    if (miningPool.Host.Contains(":"))
                        throw new ArgumentException("Invalid pool host. Please remove the port from the host.");

                    if (miningPool.WalletAddress.Length < ClassConnectorSetting.MinWalletAddressSize)
                        throw new ArgumentException("Invalid Xiropht wallet address. Please check your wallet address again.");
                }
            }
            catch (Exception ex)
            {
                LoggerService.LogErrorMessage(ex);
                LoggerService.LogMessage("Press Enter/Return to exit...");
                System.Console.ReadLine();
                Program.CancellationTokenSource.Cancel();
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