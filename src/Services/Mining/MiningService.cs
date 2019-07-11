using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using TheDialgaTeam.Microsoft.Extensions.DependencyInjection;
using TheDialgaTeam.Xiropht.Xirorig.Console;
using TheDialgaTeam.Xiropht.Xirorig.Mining;
using TheDialgaTeam.Xiropht.Xirorig.Mining.Pool;
using TheDialgaTeam.Xiropht.Xirorig.Mining.Solo;
using TheDialgaTeam.Xiropht.Xirorig.Services.Console;
using TheDialgaTeam.Xiropht.Xirorig.Services.Setting;
using TheDialgaTeam.Xiropht.Xirorig.Setting;
using Xiropht_Connector_All.Setting;

namespace TheDialgaTeam.Xiropht.Xirorig.Services.Mining
{
    public sealed class MiningService : IInitializable, ILateInitializable
    {
        public AbstractListener Listener { get; private set; }

        public AbstractMiner Miner { get; private set; }

        public decimal TotalGoodSharesSubmitted { get; private set; }

        public decimal TotalBadSharesSubmitted { get; private set; }

        private Program Program { get; }

        private LoggerService LoggerService { get; }

        private ConfigService ConfigService { get; }

        private List<AbstractListener> Listeners { get; set; }

        private List<AbstractListener> DevListeners { get; set; }

        private Stopwatch Stopwatch { get; set; }

        private int MinerRoundTimer { get; set; }

        private bool IsDevRound { get; set; }

        public MiningService(Program program, LoggerService loggerService, ConfigService configService)
        {
            Program = program;
            LoggerService = loggerService;
            ConfigService = configService;
        }

        public void Initialize()
        {
            Listeners = new List<AbstractListener>();
            DevListeners = new List<AbstractListener>();

            Stopwatch = new Stopwatch();
            Stopwatch.Stop();

            MinerRoundTimer = new Random().Next(1, 101 - ConfigService.DonateLevel);
            IsDevRound = false;

            switch (ConfigService.MiningMode)
            {
                case Config.MiningMode.Solo:
                    InitializeSolo();
                    break;

                case Config.MiningMode.SoloProxy:
                    InitializeSoloProxy();
                    break;

                case Config.MiningMode.Pool:
                    InitializePool();
                    break;

                case Config.MiningMode.PoolProxy:
                    InitializePoolProxy();
                    break;
            }
        }

        public void LateInitialize()
        {
            switch (ConfigService.MiningMode)
            {
                case Config.MiningMode.Solo:
                    LateInitializeSolo();
                    break;

                case Config.MiningMode.SoloProxy:
                    LateInitializeSoloProxy();
                    break;

                case Config.MiningMode.Pool:
                    LateInitializePool();
                    break;

                case Config.MiningMode.PoolProxy:
                    LateInitializePoolProxy();
                    break;
            }
        }

        private void InitializeSolo()
        {
            var listOfSeeds = new Dictionary<string, long>();

            foreach (var seed in ClassConnectorSetting.SeedNodeIp)
            {
                using (var ping = new Ping())
                {
                    try
                    {
                        var result = ping.Send(seed.Key);

                        if (result != null && result.Status == IPStatus.Success)
                            listOfSeeds.Add(seed.Key, result.RoundtripTime);
                    }
                    catch (Exception)
                    {
                        // Ignore this seed.
                    }
                }
            }

            var seedsToLoad = listOfSeeds.OrderBy(a => a.Value).ToDictionary(a => a.Key, a => a.Value);
            var solo = ConfigService.Solo;

            var consoleMessages = new ConsoleMessageBuilder();

            var index = 0;

            foreach (var seed in seedsToLoad)
            {
                Listeners.Add(new SoloListener(seed.Key, ClassConnectorSetting.SeedNodePort, solo.WorkerId, solo.WalletAddress));

                consoleMessages
                    .Write(" * ", ConsoleColor.Green, false)
                    .Write($"SOLO #{index + 1}".PadRight(13), false)
                    .WriteLine($"{seed.Key}:{ClassConnectorSetting.SeedNodePort}", ConsoleColor.Cyan, false);

                index++;
            }
            
            LoggerService.LogMessage(consoleMessages.Build());
        }

        private void InitializeSoloProxy()
        {
        }

        private void InitializePool()
        {
            var consoleMessages = new ConsoleMessageBuilder();
            
            for (var i = 0; i < ConfigService.Pools.Length; i++)
            {
                Listeners.Add(new PoolListener(ConfigService.Pools[i].Host, ConfigService.Pools[i].Port, ConfigService.Pools[i].WorkerId, ConfigService.Pools[i].WalletAddress));
                
                consoleMessages
                    .Write(" * ", ConsoleColor.Green, false)
                    .Write($"POOL #{i + 1}".PadRight(13), false)
                    .WriteLine($"{ConfigService.Pools[i].Host}:{ConfigService.Pools[i].Port}", ConsoleColor.Cyan, false);
            }

            LoggerService.LogMessage(consoleMessages.Build());
        }

        private void InitializePoolProxy()
        {
        }

        private void LateInitializeSolo()
        {
            Miner = new SoloMiner(Program, LoggerService, ConfigService, this);

            Program.TasksToAwait.Add(Task.Factory.StartNew(async () =>
            {
                var currentIndex = 0;
                var cancellationTokenSource = Program.CancellationTokenSource;
                var donateLevel = ConfigService.DonateLevel;
                var stopwatch = Stopwatch;
                var loggerService = LoggerService;

                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    if (Listener == null)
                    {
                        stopwatch.Restart();
                        await SwitchListener(Listeners[currentIndex]).ConfigureAwait(false);
                    }
                    else
                    {
                        if (!IsDevRound)
                        {
                            //if (donateLevel > 0)
                            //{
                            //    if (stopwatch.Elapsed.TotalMinutes >= MinerRoundTimer)
                            //    {
                            //        IsDevRound = true;
                            //        await loggerService.LogMessageAsync("Switching to Dev Round...", ConsoleColor.Yellow).ConfigureAwait(false);
                            //        await SwitchListener(DevListeners[0]).ConfigureAwait(false);
                            //        stopwatch.Restart();
                            //        continue;
                            //    }
                            //}

                            if (Listener.RetryCount >= 5)
                            {
                                currentIndex++;

                                if (currentIndex > Listeners.Count - 1)
                                    currentIndex = 0;

                                await SwitchListener(Listeners[currentIndex]).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            if (Stopwatch.Elapsed.TotalMinutes >= donateLevel)
                            {
                                IsDevRound = false;
                                MinerRoundTimer = 100 - ConfigService.DonateLevel;
                                await loggerService.LogMessageAsync("Switching to Miner Round...", ConsoleColor.Yellow).ConfigureAwait(false);
                                await SwitchListener(Listeners[currentIndex]).ConfigureAwait(false);
                                Stopwatch.Restart();
                                continue;
                            }
                        }
                    }

                    await Task.Delay(1000, cancellationTokenSource.Token).ConfigureAwait(false);
                }

                foreach (var poolListener in Listeners)
                    await poolListener.StopConnectToNetworkAsync().ConfigureAwait(false);

                //await DevListeners[0].StopConnectToNetworkAsync().ConfigureAwait(false);
            }, Program.CancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).Unwrap());
        }

        private void LateInitializeSoloProxy()
        {
        }

        private void LateInitializePool()
        {
            Miner = new PoolMiner(Program, LoggerService, ConfigService, this);

            Program.TasksToAwait.Add(Task.Factory.StartNew(async () =>
            {
                var currentIndex = 0;
                var cancellationTokenSource = Program.CancellationTokenSource;
                var donateLevel = ConfigService.DonateLevel;
                var stopwatch = Stopwatch;
                var loggerService = LoggerService;

                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    if (Listener == null)
                    {
                        stopwatch.Restart();
                        await SwitchListener(Listeners[currentIndex]).ConfigureAwait(false);
                    }
                    else
                    {
                        if (!IsDevRound)
                        {
                            //if (donateLevel > 0)
                            //{
                            //    if (stopwatch.Elapsed.TotalMinutes >= MinerRoundTimer)
                            //    {
                            //        IsDevRound = true;
                            //        await loggerService.LogMessageAsync("Switching to Dev Round...", ConsoleColor.Yellow).ConfigureAwait(false);
                            //        await SwitchListener(DevListeners[0]).ConfigureAwait(false);
                            //        stopwatch.Restart();
                            //        continue;
                            //    }
                            //}

                            if (Listener.RetryCount >= 5)
                            {
                                currentIndex++;

                                if (currentIndex > Listeners.Count - 1)
                                    currentIndex = 0;

                                await SwitchListener(Listeners[currentIndex]).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            if (Stopwatch.Elapsed.TotalMinutes >= donateLevel)
                            {
                                IsDevRound = false;
                                MinerRoundTimer = 100 - ConfigService.DonateLevel;
                                await loggerService.LogMessageAsync("Switching to Miner Round...", ConsoleColor.Yellow).ConfigureAwait(false);
                                await SwitchListener(Listeners[currentIndex]).ConfigureAwait(false);
                                Stopwatch.Restart();
                                continue;
                            }
                        }
                    }

                    await Task.Delay(1000, cancellationTokenSource.Token).ConfigureAwait(false);
                }

                foreach (var poolListener in Listeners)
                    await poolListener.StopConnectToNetworkAsync().ConfigureAwait(false);

                //await DevListeners[0].StopConnectToNetworkAsync().ConfigureAwait(false);
            }, Program.CancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).Unwrap());
        }

        private void LateInitializePoolProxy()
        {
        }

        private async Task SwitchListener(AbstractListener listener)
        {
            if (Listener != null)
            {
                Listener.Disconnected -= ListenerOnDisconnected;
                Listener.LoginResult -= ListenerOnLoginResult;
                Listener.NewJob -= ListenerOnNewJob;
                Listener.ShareResult -= ListenerOnShareResult;

                await Listener.StopConnectToNetworkAsync().ConfigureAwait(false);
            }

            Listener = listener;
            Listener.Disconnected += ListenerOnDisconnected;
            Listener.LoginResult += ListenerOnLoginResult;
            Listener.NewJob += ListenerOnNewJob;
            Listener.ShareResult += ListenerOnShareResult;

            await Listener.StartConnectToNetworkAsync().ConfigureAwait(false);
        }

        private async Task ListenerOnDisconnected(AbstractListener listener)
        {
            await LoggerService.LogMessageAsync($"[{listener.Host}:{listener.Port}] Disconnected", ConsoleColor.Red).ConfigureAwait(false);
        }

        private async Task ListenerOnLoginResult(AbstractListener listener, bool success)
        {
            if (success)
            {
                var listenerType = "";

                switch (listener)
                {
                    case PoolListener _:
                        listenerType = "pool";
                        break;

                    case SoloListener _:
                        listenerType = "solo";
                        break;
                }

                await LoggerService.LogMessageAsync(new ConsoleMessageBuilder()
                    .Write($"use {listenerType} ", true)
                    .WriteLine($"{listener.Host}:{listener.Port}", ConsoleColor.Cyan, false)
                    .Build()).ConfigureAwait(false);
            }
            else
                await LoggerService.LogMessageAsync($"[{listener.Host}:{listener.Port}] Login failed. Reason: Invalid wallet address.", ConsoleColor.Red).ConfigureAwait(false);
        }

        private async Task ListenerOnNewJob(AbstractListener listener, string packet)
        {
            Miner.UpdateJob(packet);

            await LoggerService.LogMessageAsync(new ConsoleMessageBuilder()
                .Write("new job ", ConsoleColor.Magenta, true)
                .WriteLine($"from {listener.Host}:{listener.Port} diff {Miner.JobDifficulty}/{Miner.BlockDifficulty} algo {Miner.JobMethodName} height {Miner.BlockId}", false)
                .Build()).ConfigureAwait(false);
        }

        private async Task ListenerOnShareResult(AbstractListener listener, bool accepted, string reason)
        {
            if (accepted)
            {
                TotalGoodSharesSubmitted++;

                await LoggerService.LogMessageAsync(new ConsoleMessageBuilder()
                    .Write("accepted ", ConsoleColor.Green, true)
                    .WriteLine($"({TotalGoodSharesSubmitted}/{TotalBadSharesSubmitted})", false)
                    .Build()).ConfigureAwait(false);
            }
            else
            {
                TotalBadSharesSubmitted++;

                await LoggerService.LogMessageAsync(new ConsoleMessageBuilder()
                    .Write("rejected ", ConsoleColor.Red, true)
                    .WriteLine($"({TotalGoodSharesSubmitted}/{TotalBadSharesSubmitted}) - {reason}", false)
                    .Build()).ConfigureAwait(false);
            }
        }
    }
}