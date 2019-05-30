using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TheDialgaTeam.Microsoft.Extensions.DependencyInjection;
using TheDialgaTeam.Xiropht.Xirorig.Services.Console;
using TheDialgaTeam.Xiropht.Xirorig.Services.Pool.Packet;
using TheDialgaTeam.Xiropht.Xirorig.Services.Setting;

namespace TheDialgaTeam.Xiropht.Xirorig.Services.Pool
{
    public sealed class PoolService : ILateInitializable, IDisposable
    {
        public bool IsConnected { get; private set; }

        public bool IsLoggedIn { get; private set; }

        public PoolMiner PoolMiner { get; private set; }

        public ulong TotalGoodSharesSubmitted { get; private set; }

        public ulong TotalBadSharesSubmitted { get; private set; }

        public CancellationTokenSource CancellationTokenSource { get; private set; }

        private Program Program { get; }

        private LoggerService LoggerService { get; }

        private ConfigService ConfigService { get; }

        private Task ReadPacketFromPoolNetworkTask { get; set; }

        private string Host { get; set; }

        private ushort Port { get; set; }

        private TcpClient PoolClient { get; set; }

        private StreamReader PoolClientReader { get; set; }

        private StreamWriter PoolClientWriter { get; set; }

        private DateTimeOffset LastValidPacketBeforeTimeout { get; set; }

        private SemaphoreSlim SemaphoreSlim { get; set; }

        public PoolService(Program program, LoggerService loggerService, ConfigService configService)
        {
            Program = program;
            LoggerService = loggerService;
            ConfigService = configService;
        }

        public void LateInitialize()
        {
            PoolMiner = new PoolMiner(LoggerService, ConfigService, this);
            SemaphoreSlim = new SemaphoreSlim(1, 1);

            Program.TasksToAwait.Add(Task.Factory.StartNew(async () =>
            {
                var retry = 0;
                var currentPoolIndex = 0;

                while (!Program.CancellationTokenSource.IsCancellationRequested)
                {
                    if (!IsConnected)
                    {
                        if (retry > 4)
                        {
                            retry = 0;
                            currentPoolIndex = Math.Min(ConfigService.Pools.Length, currentPoolIndex + 1);
                        }

                        var miningPool = ConfigService.Pools[currentPoolIndex];
                        Host = miningPool.Host.Remove(miningPool.Host.IndexOf(':'));
                        Port = ushort.Parse(miningPool.Host.Substring(miningPool.Host.IndexOf(':') + 1));

                        await ConnectToPoolNetwork(Host, Port, miningPool.WalletAddress, miningPool.WorkerId).ConfigureAwait(false);

                        if (IsConnected)
                        {
                            retry = 0;
                            currentPoolIndex = 0;
                        }
                        else
                        {
                            await Task.Delay(5000).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        if (IsLoggedIn && DateTimeOffset.Now >= LastValidPacketBeforeTimeout)
                        {
                            IsConnected = false;
                            IsLoggedIn = false;

                            await LoggerService.LogMessageAsync($"[{Host}:{Port}] Lost connection to the pool.", ConsoleColor.Red).ConfigureAwait(false);
                        }
                        else
                        {
                            await Task.Delay(1000).ConfigureAwait(false);
                        }
                    }
                }

                await DisconnectPoolNetworkAsync().ConfigureAwait(false);
            }, Program.CancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).Unwrap());
        }

        public async Task SendPacketToPoolNetworkAsync(string packet)
        {
            try
            {
                await SemaphoreSlim.WaitAsync(CancellationTokenSource.Token).ConfigureAwait(false);
                await PoolClientWriter.WriteLineAsync(packet).ConfigureAwait(false);
                await PoolClientWriter.FlushAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                IsConnected = false;
                IsLoggedIn = false;
                LoggerService.LogMessage($"[{Host}:{Port}] Lost connection to the pool.", ConsoleColor.Red);
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        private async Task ConnectToPoolNetwork(string host, ushort port, string walletAddress, string workerId)
        {
            if (IsConnected)
                return;

            IsConnected = false;
            IsLoggedIn = false;

            CancellationTokenSource?.Dispose();
            CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(Program.CancellationTokenSource.Token);

            try
            {
                PoolClient?.Dispose();
                PoolClient = new TcpClient();

                var connectTask = PoolClient.ConnectAsync(host, port);
                var timeoutTask = Task.Delay(5000, CancellationTokenSource.Token);

                await Task.WhenAny(connectTask, timeoutTask).ConfigureAwait(false);

                if (!connectTask.IsCompleted)
                    throw new Exception();

                IsConnected = true;
                LastValidPacketBeforeTimeout = DateTimeOffset.Now.AddSeconds(5);

                PoolClientReader?.Dispose();
                PoolClientWriter?.Dispose();

                PoolClientReader = new StreamReader(PoolClient.GetStream());
                PoolClientWriter = new StreamWriter(PoolClient.GetStream());

                await WaitForTasksToCompleteAsync().ConfigureAwait(false);

                ReadPacketFromPoolNetworkTask = Task.Factory.StartNew(async () =>
                {
                    while (!CancellationTokenSource.IsCancellationRequested && IsConnected)
                    {
                        try
                        {
                            var packet = await PoolClientReader.ReadLineAsync().ConfigureAwait(false);
                            _ = Task.Run(async () => await HandlePacketFromPoolNetworkAsync(packet).ConfigureAwait(false), CancellationTokenSource.Token);
                        }
                        catch (Exception)
                        {
                            IsConnected = false;
                            IsLoggedIn = false;
                            LoggerService.LogMessage($"[{host}:{port}] Lost connection to the pool.", ConsoleColor.Red);
                        }
                    }
                }, CancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).Unwrap();

                var consoleMessage = new ConsoleMessageBuilder()
                    .Write("use pool ", includeDateTime: true)
                    .WriteLine($"{host}:{port}", ConsoleColor.Cyan, false);

                await LoggerService.LogMessageAsync(consoleMessage.Build()).ConfigureAwait(false);

                var loginPacket = new JObject
                {
                    { PoolPacket.Type, PoolPacketType.Login },
                    { PoolLoginPacket.WalletAddress, walletAddress },
                    { PoolLoginPacket.Version, $"Xirorig/{Assembly.GetExecutingAssembly().GetName().Version} {Assembly.GetExecutingAssembly().GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName}" },
                    { PoolLoginPacket.WorkerId, workerId }
                };

                await SendPacketToPoolNetworkAsync(loginPacket.ToString(Formatting.None)).ConfigureAwait(false);
            }
            catch (Exception)
            {
                IsConnected = false;
                IsLoggedIn = false;

                await LoggerService.LogMessageAsync($"[{host}:{port}] Unable to connect or connection timeout.", ConsoleColor.Red).ConfigureAwait(false);
            }
        }

        private async Task DisconnectPoolNetworkAsync()
        {
            CancellationTokenSource?.Cancel();
            await WaitForTasksToCompleteAsync().ConfigureAwait(false);
        }

        private async Task WaitForTasksToCompleteAsync()
        {
            var taskToWait = new List<Task>();

            if (ReadPacketFromPoolNetworkTask != null)
                taskToWait.Add(ReadPacketFromPoolNetworkTask);

            if (taskToWait.Count > 0)
                await Task.WhenAll(taskToWait).ConfigureAwait(false);
        }

        private async Task HandlePacketFromPoolNetworkAsync(string packet)
        {
            try
            {
                var jsonPacket = JObject.Parse(packet);
                var type = jsonPacket[PoolPacket.Type].ToString();

                if (string.Equals(type, PoolPacketType.Login, StringComparison.OrdinalIgnoreCase))
                {
                    LastValidPacketBeforeTimeout = DateTimeOffset.Now.AddSeconds(5);

                    if (jsonPacket.ContainsKey(PoolLoginPacket.LoginWrong))
                    {
                        IsLoggedIn = false;
                        IsConnected = false;
                        await LoggerService.LogMessageAsync("Wrong login/wallet address, please check your setting. Disconnect now.", ConsoleColor.Red).ConfigureAwait(false);
                        CancellationTokenSource.Cancel();
                    }

                    if (jsonPacket.ContainsKey(PoolLoginPacket.LoginOkay))
                    {
                        IsLoggedIn = true;
                        PoolMiner.StartMining();
                    }
                }

                if (string.Equals(type, PoolPacketType.KeepAlive, StringComparison.OrdinalIgnoreCase))
                    LastValidPacketBeforeTimeout = DateTimeOffset.Now.AddSeconds(5);

                if (string.Equals(type, PoolPacketType.Job, StringComparison.OrdinalIgnoreCase))
                {
                    LastValidPacketBeforeTimeout = DateTimeOffset.Now.AddSeconds(5);
                    PoolMiner.UpdateJob(packet);

                    var consoleMessage = new ConsoleMessageBuilder()
                        .Write("new job ", ConsoleColor.Magenta, true)
                        .WriteLine($"from {Host}:{Port} diff {PoolMiner.JobDifficulty} algo {PoolMiner.JobMethodName} height {PoolMiner.BlockId}", includeDateTime: false);
                    
                    await LoggerService.LogMessageAsync(consoleMessage.Build()).ConfigureAwait(false);
                }

                if (string.Equals(type, PoolPacketType.Share, StringComparison.OrdinalIgnoreCase))
                {
                    LastValidPacketBeforeTimeout = DateTimeOffset.Now.AddSeconds(5);

                    var result = jsonPacket[PoolSharePacket.Result].ToString();

                    if (string.Equals(result, PoolSharePacket.ResultShareOk, StringComparison.OrdinalIgnoreCase))
                    {
                        TotalGoodSharesSubmitted++;

                        var consoleMessage = new ConsoleMessageBuilder()
                            .Write("accepted ", ConsoleColor.Green, true)
                            .WriteLine($"({TotalGoodSharesSubmitted}/{TotalBadSharesSubmitted}) diff {PoolMiner.JobDifficulty}", includeDateTime: false);

                        await LoggerService.LogMessageAsync(consoleMessage.Build()).ConfigureAwait(false);
                    }

                    if (string.Equals(result, PoolSharePacket.ResultShareInvalid, StringComparison.OrdinalIgnoreCase))
                    {
                        TotalBadSharesSubmitted++;

                        var consoleMessage = new ConsoleMessageBuilder()
                            .Write("rejected ", ConsoleColor.Red, true)
                            .WriteLine($"({TotalGoodSharesSubmitted}/{TotalBadSharesSubmitted}) diff {PoolMiner.JobDifficulty}", includeDateTime: false);

                        await LoggerService.LogMessageAsync(consoleMessage.Build()).ConfigureAwait(false);
                    }

                    if (string.Equals(result, PoolSharePacket.ResultShareDuplicate, StringComparison.OrdinalIgnoreCase))
                    {
                        TotalBadSharesSubmitted++;

                        var consoleMessage = new ConsoleMessageBuilder()
                            .Write("rejected ", ConsoleColor.Red, true)
                            .WriteLine($"({TotalGoodSharesSubmitted}/{TotalBadSharesSubmitted}) diff {PoolMiner.JobDifficulty}", includeDateTime: false);

                        await LoggerService.LogMessageAsync(consoleMessage.Build()).ConfigureAwait(false);
                    }

                    if (string.Equals(result, PoolSharePacket.ResultShareLowDifficulty, StringComparison.OrdinalIgnoreCase))
                    {
                        TotalBadSharesSubmitted++;

                        var consoleMessage = new ConsoleMessageBuilder()
                            .Write("rejected ", ConsoleColor.Red, true)
                            .WriteLine($"({TotalGoodSharesSubmitted}/{TotalBadSharesSubmitted}) diff {PoolMiner.JobDifficulty}", includeDateTime: false);

                        await LoggerService.LogMessageAsync(consoleMessage.Build()).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception)
            {
                // Invalid packets received. Maybe ignore this?
            }
        }

        public void Dispose()
        {
            CancellationTokenSource?.Dispose();
            ReadPacketFromPoolNetworkTask?.Dispose();
            PoolClient?.Dispose();
            PoolClientReader?.Dispose();
            PoolClientWriter?.Dispose();
            PoolMiner?.Dispose();
            SemaphoreSlim?.Dispose();
        }
    }
}