using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using TheDialgaTeam.Microsoft.Extensions.DependencyInjection;
using TheDialgaTeam.Xiropht.Xirorig.Services.Console;
using TheDialgaTeam.Xiropht.Xirorig.Services.Setting;

namespace TheDialgaTeam.Xiropht.Xirorig.Services.Pool
{
    public sealed class PoolService : IInitializable, ILateInitializable
    {
        //public PoolMiner PoolMiner { get; private set; }

        public ulong TotalGoodSharesSubmitted { get; private set; }

        public ulong TotalBadSharesSubmitted { get; private set; }

        private Program Program { get; }

        private LoggerService LoggerService { get; }

        private ConfigService ConfigService { get; }

        private List<PoolListener> PoolListeners { get; set; }

        private PoolListener CurrentPoolListener { get; set; }

        private PoolListener DevPoolListener { get; set; }

        private Stopwatch Stopwatch { get; set; }

        private int DevRoundTimer { get; set; }

        private int MinerRoundTimer { get; set; }

        private bool IsDevRound { get; set; }

        public PoolService(Program program, LoggerService loggerService, ConfigService configService)
        {
            Program = program;
            LoggerService = loggerService;
            ConfigService = configService;
        }

        public void Initialize()
        {
            PoolListeners = new List<PoolListener>();

            foreach (var miningPool in ConfigService.Pools)
                PoolListeners.Add(new PoolListener(miningPool.Host, miningPool.Port, miningPool.WalletAddress, miningPool.WorkerId));

            DevPoolListener = new PoolListener("pool.xiro.aggressivegaming.org", 4446, "brjxVl3Ge1Sv60vXFDarqqQds5cxiS4Bec2p4Zld1v1yvslgY7caGebIF83mSb", "DEV_FEE");

            Stopwatch = new Stopwatch();
            Stopwatch.Stop();

            MinerRoundTimer = new Random().Next(1, 101 - ConfigService.DonateLevel);
            DevRoundTimer = ConfigService.DonateLevel;
            IsDevRound = false;
        }

        public void LateInitialize()
        {
            Program.TasksToAwait.Add(Task.Factory.StartNew(async () =>
            {
                var retryCount = 0;
                var currentIndex = 0;

                while (!Program.CancellationTokenSource.IsCancellationRequested)
                {
                    if (CurrentPoolListener == null)
                    {
                        Stopwatch.Restart();
                        await SwitchPoolListener(PoolListeners[currentIndex]).ConfigureAwait(false);
                    }
                    else
                    {
                        if (!IsDevRound)
                        {
                            if (Stopwatch.Elapsed.TotalMinutes >= MinerRoundTimer)
                            {
                                IsDevRound = true;
                                await LoggerService.LogMessageAsync("Switching to Dev Round...", ConsoleColor.Yellow).ConfigureAwait(false);
                                await SwitchPoolListener(DevPoolListener).ConfigureAwait(false);
                                Stopwatch.Restart();
                                continue;
                            }

                            if (CurrentPoolListener.ConnectionStatus == ConnectionStatus.Disconnected)
                            {
                                retryCount++;

                                if (retryCount > 5)
                                {
                                    currentIndex++;

                                    if (currentIndex > PoolListeners.Count - 1)
                                        currentIndex = 0;
                                }

                                await SwitchPoolListener(PoolListeners[currentIndex]).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            if (Stopwatch.Elapsed.TotalMinutes >= DevRoundTimer)
                            {
                                IsDevRound = false;
                                MinerRoundTimer = 100 - ConfigService.DonateLevel;
                                await LoggerService.LogMessageAsync("Switching to Miner Round...", ConsoleColor.Yellow).ConfigureAwait(false);
                                await SwitchPoolListener(PoolListeners[currentIndex]).ConfigureAwait(false);
                                Stopwatch.Restart();
                                continue;
                            }
                        }
                    }

                    await Task.Delay(1000).ConfigureAwait(false);
                }

                foreach (var poolListener in PoolListeners)
                    await poolListener.StopConnectToNetwork().ConfigureAwait(false);

                await DevPoolListener.StopConnectToNetwork().ConfigureAwait(false);
            }, Program.CancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).Unwrap());
        }

        private async Task SwitchPoolListener(PoolListener poolListener)
        {
            if (CurrentPoolListener != null)
            {
                CurrentPoolListener.Disconnected -= CurrentPoolListenerOnDisconnected;
                CurrentPoolListener.LoginResult -= CurrentPoolListenerOnLoginResult;
                CurrentPoolListener.NewJob -= CurrentPoolListenerOnNewJob;
                CurrentPoolListener.ShareAccepted -= CurrentPoolListenerOnShareAccepted;
                CurrentPoolListener.ShareRejected -= CurrentPoolListenerOnShareRejected;

                await CurrentPoolListener.StopConnectToNetwork().ConfigureAwait(false);
            }

            CurrentPoolListener = poolListener;
            CurrentPoolListener.Disconnected += CurrentPoolListenerOnDisconnected;
            CurrentPoolListener.LoginResult += CurrentPoolListenerOnLoginResult;
            CurrentPoolListener.NewJob += CurrentPoolListenerOnNewJob;
            CurrentPoolListener.ShareAccepted += CurrentPoolListenerOnShareAccepted;
            CurrentPoolListener.ShareRejected += CurrentPoolListenerOnShareRejected;

            await CurrentPoolListener.StartConnectToNetwork().ConfigureAwait(false);
        }

        private async Task CurrentPoolListenerOnDisconnected(PoolListener poolListener)
        {
            await LoggerService.LogMessageAsync($"[{poolListener.Host}:{poolListener.Port}] Disconnected", ConsoleColor.Red).ConfigureAwait(false);
        }

        private async Task CurrentPoolListenerOnLoginResult(PoolListener poolListener, bool success)
        {
            if (success)
            {
                await LoggerService.LogMessageAsync(new ConsoleMessageBuilder()
                    .Write("use pool ", includeDateTime: true)
                    .WriteLine($"{poolListener.Host}:{poolListener.Port}", ConsoleColor.Cyan, false)
                    .Build()).ConfigureAwait(false);
            }
            else
                await LoggerService.LogMessageAsync($"[{poolListener.Host}:{poolListener.Port}] Login failed. Reason: Invalid wallet address.", ConsoleColor.Red).ConfigureAwait(false);
        }

        private async Task CurrentPoolListenerOnNewJob(PoolListener poolListener, string packet)
        {
            
        }

        private async Task CurrentPoolListenerOnShareAccepted(PoolListener poolListener)
        {
            
        }

        private async Task CurrentPoolListenerOnShareRejected(PoolListener poolListener, string arg2)
        {
            
        }
    }
}