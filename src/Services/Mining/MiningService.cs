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
    public sealed class MiningService : IInitializable, ILateInitializable, IDisposable
    {
        public AbstractListener Listener { get; private set; }

        public AbstractMiner Miner { get; private set; }

        public decimal TotalGoodSharesSubmitted { get; private set; }

        public decimal TotalBadSharesSubmitted { get; private set; }

        private LoggerService LoggerService { get; }

        private ConfigService ConfigService { get; }

        private List<AbstractListener> Listeners { get; set; }

        private List<AbstractListener> DevListeners { get; set; }

        private Stopwatch Stopwatch { get; set; }

        private int MinerRoundTimer { get; set; }

        private bool IsDevRound { get; set; }

        public MiningService(LoggerService loggerService, ConfigService configService)
        {
            LoggerService = loggerService;
            ConfigService = configService;
        }

        public void Initialize()
        {
            Listeners = new List<AbstractListener>();
            DevListeners = new List<AbstractListener>();

            Stopwatch = new Stopwatch();
            Stopwatch.Stop();

            var configService = ConfigService;

            MinerRoundTimer = new Random().Next(1, 101 - configService.DonateLevel);
            IsDevRound = false;

            switch (configService.MiningMode)
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

            var seedsToLoad = listOfSeeds.OrderBy(a => a.Value);
            var listeners = Listeners;
            var solo = ConfigService.Solo;

            var consoleMessages = new ConsoleMessageBuilder();
            var index = 0;

            foreach (var seed in seedsToLoad)
            {
                listeners.Add(new SoloListener(seed.Key, ClassConnectorSetting.SeedNodePort, solo.WalletAddress));

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
            throw new NotImplementedException();
        }

        private void InitializePool()
        {
            var consoleMessages = new ConsoleMessageBuilder();
            var pools = ConfigService.Pools;
            var listeners = Listeners;
            
            for (var i = 0; i < pools.Length; i++)
            {
                listeners.Add(new PoolListener(pools[i].Host, pools[i].Port, pools[i].WalletAddress, pools[i].WorkerId));
                
                consoleMessages
                    .Write(" * ", ConsoleColor.Green, false)
                    .Write($"POOL #{i + 1}".PadRight(13), false)
                    .WriteLine($"{pools[i].Host}:{pools[i].Port}", ConsoleColor.Cyan, false);
            }

            LoggerService.LogMessage(consoleMessages.Build());
        }

        private void InitializePoolProxy()
        {
            throw new NotImplementedException();
        }

        private void LateInitializeSolo()
        {
            Miner = new SoloMiner(LoggerService, ConfigService, this);

            Program.TasksToAwait.Add(Task.Run(async () =>
            {
                var currentIndex = 0;
                var cancellationTokenSource = Program.CancellationTokenSource;
                var listeners = Listeners;

                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    if (Listener == null)
                    {
                        await SwitchListener(listeners[currentIndex]).ConfigureAwait(false);
                    }
                    else if (Listener.RetryCount >= 5)
                    {
                        currentIndex++;

                        if (currentIndex > listeners.Count - 1)
                            currentIndex = 0;

                        await SwitchListener(listeners[currentIndex]).ConfigureAwait(false);
                    }

                    await Task.Delay(1000, cancellationTokenSource.Token).ConfigureAwait(false);
                }

                foreach (var listener in listeners)
                    await listener.StopConnectToNetworkAsync().ConfigureAwait(false);
            }, Program.CancellationTokenSource.Token));
        }

        private void LateInitializeSoloProxy()
        {
            throw new NotImplementedException();
        }

        private void LateInitializePool()
        {
            Miner = new PoolMiner(LoggerService, ConfigService, this);

            Program.TasksToAwait.Add(Task.Run(async () =>
            {
                var currentIndex = 0;
                var cancellationTokenSource = Program.CancellationTokenSource;
                var listeners = Listeners;

                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    if (Listener == null)
                    {
                        await SwitchListener(listeners[currentIndex]).ConfigureAwait(false);
                    }
                    else if (Listener.RetryCount >= 5)
                    {
                        currentIndex++;

                        if (currentIndex > listeners.Count - 1)
                            currentIndex = 0;

                        await SwitchListener(listeners[currentIndex]).ConfigureAwait(false);
                    }

                    await Task.Delay(1000, cancellationTokenSource.Token).ConfigureAwait(false);
                }

                foreach (var listener in listeners)
                    await listener.StopConnectToNetworkAsync().ConfigureAwait(false);
            }, Program.CancellationTokenSource.Token));
        }

        private void LateInitializePoolProxy()
        {
            throw new NotImplementedException();
        }

        private async Task SwitchListener(AbstractListener listener)
        {
            var currentListener = Listener;

            if (currentListener != null)
            {
                currentListener.Disconnected -= ListenerOnDisconnected;
                currentListener.LoginResult -= ListenerOnLoginResult;
                currentListener.NewJob -= ListenerOnNewJob;
                currentListener.ShareResult -= ListenerOnShareResult;

                await currentListener.StopConnectToNetworkAsync().ConfigureAwait(false);
            }

            Listener = listener;
            Listener.Disconnected += ListenerOnDisconnected;
            Listener.LoginResult += ListenerOnLoginResult;
            Listener.NewJob += ListenerOnNewJob;
            Listener.ShareResult += ListenerOnShareResult;

            await Listener.StartConnectToNetworkAsync().ConfigureAwait(false);
        }

        private void ListenerOnDisconnected(AbstractListener listener)
        {
            LoggerService.LogMessage($"[{listener.Host}:{listener.Port}] Disconnected", ConsoleColor.Red);
        }

        private void ListenerOnLoginResult(AbstractListener listener, bool success)
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

                LoggerService.LogMessage(new ConsoleMessageBuilder()
                    .Write($"use {listenerType} ", true)
                    .WriteLine($"{listener.Host}:{listener.Port}", ConsoleColor.Cyan, false)
                    .Build());
            }
            else
                LoggerService.LogMessage($"[{listener.Host}:{listener.Port}] Login failed. Reason: Invalid wallet address.", ConsoleColor.Red);
        }

        private void ListenerOnNewJob(AbstractListener listener, string packet)
        {
            var miner = Miner;
            miner.UpdateJob(packet);

            switch (listener)
            {
                case SoloListener _:
                    LoggerService.LogMessage(new ConsoleMessageBuilder()
                        .Write("new job ", ConsoleColor.Magenta, true)
                        .WriteLine($"from {listener.Host}:{listener.Port} diff {miner.BlockDifficulty} algo {miner.JobMethodName} height {miner.BlockId}", false)
                        .Build());
                    break;

                case PoolListener _:
                    LoggerService.LogMessage(new ConsoleMessageBuilder()
                        .Write("new job ", ConsoleColor.Magenta, true)
                        .WriteLine($"from {listener.Host}:{listener.Port} diff {miner.JobDifficulty}/{miner.BlockDifficulty} algo {miner.JobMethodName} height {miner.BlockId}", false)
                        .Build());
                    break;
            }
        }

        private void ListenerOnShareResult(AbstractListener listener, bool accepted, string reason)
        {
            if (accepted)
            {
                TotalGoodSharesSubmitted++;

                LoggerService.LogMessage(new ConsoleMessageBuilder()
                    .Write("accepted ", ConsoleColor.Green, true)
                    .WriteLine($"({TotalGoodSharesSubmitted}/{TotalBadSharesSubmitted})", false)
                    .Build());
            }
            else
            {
                TotalBadSharesSubmitted++;

                LoggerService.LogMessage(new ConsoleMessageBuilder()
                    .Write("rejected ", ConsoleColor.Red, true)
                    .WriteLine($"({TotalGoodSharesSubmitted}/{TotalBadSharesSubmitted}) - {reason}", false)
                    .Build());
            }
        }

        public void Dispose()
        {
            Miner?.Dispose();
        }
    }
}