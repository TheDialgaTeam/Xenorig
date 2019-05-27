using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
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
    public sealed class PoolService : IInitializable, IDisposable
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

        public void Initialize()
        {
            PoolMiner = new PoolMiner(LoggerService, ConfigService, this);
            SemaphoreSlim = new SemaphoreSlim(1, 1);
            Program.TasksToAwait.Add(ConnectToPoolNetworkUntilCancellationAsync());
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
                LoggerService.LogMessage("Lost connection to the pool.", ConsoleColor.Red);
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        private async Task ConnectToPoolNetworkUntilCancellationAsync()
        {
            while (!Program.CancellationTokenSource.IsCancellationRequested)
            {
                await ConnectToPoolNetwork(ConfigService.Host, ConfigService.Port, ConfigService.WalletAddress).ConfigureAwait(false);

                if (IsLoggedIn && DateTimeOffset.Now >= LastValidPacketBeforeTimeout)
                {
                    IsConnected = false;
                    IsLoggedIn = false;
                    LoggerService.LogMessage("Lost connection to the pool.", ConsoleColor.Red);
                }

                await Task.Delay(1000).ConfigureAwait(false);
            }

            await DisconnectPoolNetworkAsync().ConfigureAwait(false);
        }

        private async Task ConnectToPoolNetwork(string host, ushort port, string walletAddress)
        {
            if (IsConnected)
                return;

            IsConnected = false;
            IsLoggedIn = false;

            CancellationTokenSource?.Dispose();
            CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(Program.CancellationTokenSource.Token);

            await LoggerService.LogMessageAsync($"Attempt to connect to the pool at {host}:{port}").ConfigureAwait(false);

            try
            {
                PoolClient?.Dispose();
                PoolClientReader?.Dispose();
                PoolClientWriter?.Dispose();

                PoolClient = new TcpClient();

                var connectTask = PoolClient.ConnectAsync(host, port);
                var timeoutTask = Task.Delay(5000);

                await Task.WhenAny(connectTask, timeoutTask).ConfigureAwait(false);

                if (timeoutTask.IsCompleted)
                    throw new Exception("Connection timeout.");

                IsConnected = true;
                LastValidPacketBeforeTimeout = DateTimeOffset.Now.AddSeconds(5);

                PoolClientReader = new StreamReader(PoolClient.GetStream());
                PoolClientWriter = new StreamWriter(PoolClient.GetStream());

                await WaitForTasksToCompleteAsync().ConfigureAwait(false);

                ReadPacketFromPoolNetworkTask = ReadPacketFromPoolNetworkAsync();
                Program.TasksToAwait.Add(ReadPacketFromPoolNetworkTask);

                await LoggerService.LogMessageAsync("Pool is connected.", ConsoleColor.Green).ConfigureAwait(false);

                var loginPacket = new JObject
                {
                    { PoolPacket.Type, PoolPacketType.Login },
                    { PoolLoginPacket.WalletAddress, walletAddress },
                    { PoolLoginPacket.Version, $"Xirorig (.Net Core) v{Assembly.GetExecutingAssembly().GetName().Version}" }
                };

                await SendPacketToPoolNetworkAsync(loginPacket.ToString(Formatting.None)).ConfigureAwait(false);
            }
            catch (Exception)
            {
                IsConnected = false;
                IsLoggedIn = false;
                await LoggerService.LogMessageAsync("Unable to connect to the pool. Trying again in 5 seconds...", ConsoleColor.Red).ConfigureAwait(false);

                await Task.Delay(4000).ConfigureAwait(false);
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

        private async Task ReadPacketFromPoolNetworkAsync()
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
                    LoggerService.LogMessage("Lost connection to the pool.", ConsoleColor.Red);
                }
            }
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
                        await LoggerService.LogMessageAsync("Login/Wallet Address accepted by the pool. Waiting for job.", ConsoleColor.Green).ConfigureAwait(false);
                    }
                }

                if (string.Equals(type, PoolPacketType.KeepAlive, StringComparison.OrdinalIgnoreCase))
                    LastValidPacketBeforeTimeout = DateTimeOffset.Now.AddSeconds(5);

                if (string.Equals(type, PoolPacketType.Job, StringComparison.OrdinalIgnoreCase))
                {
                    LastValidPacketBeforeTimeout = DateTimeOffset.Now.AddSeconds(5);
                    PoolMiner.UpdateJob(packet);
                    await LoggerService.LogMessageAsync($"New Mining Job | Block ID: {PoolMiner.BlockId} | Block Difficulty: {PoolMiner.BlockDifficulty} | Job Difficulty: {PoolMiner.JobDifficulty} | Job Share(s) to find: {PoolMiner.TotalShareToFind}", ConsoleColor.Magenta).ConfigureAwait(false);
                }

                if (string.Equals(type, PoolPacketType.Share, StringComparison.OrdinalIgnoreCase))
                {
                    LastValidPacketBeforeTimeout = DateTimeOffset.Now.AddSeconds(5);

                    var result = jsonPacket[PoolSharePacket.Result].ToString();

                    if (string.Equals(result, PoolSharePacket.ResultShareOk, StringComparison.OrdinalIgnoreCase))
                    {
                        TotalGoodSharesSubmitted++;
                        await LoggerService.LogMessageAsync($"Good Share! [Total = {TotalGoodSharesSubmitted}]", ConsoleColor.Green).ConfigureAwait(false);
                    }

                    if (string.Equals(result, PoolSharePacket.ResultShareInvalid, StringComparison.OrdinalIgnoreCase))
                    {
                        TotalBadSharesSubmitted++;
                        await LoggerService.LogMessageAsync($"Invalid Share! [Total = {TotalBadSharesSubmitted}]", ConsoleColor.Red).ConfigureAwait(false);
                    }

                    if (string.Equals(result, PoolSharePacket.ResultShareDuplicate, StringComparison.OrdinalIgnoreCase))
                    {
                        TotalBadSharesSubmitted++;
                        await LoggerService.LogMessageAsync($"Duplicate Share! [Total = {TotalBadSharesSubmitted}]", ConsoleColor.Red).ConfigureAwait(false);
                    }

                    if (string.Equals(result, PoolSharePacket.ResultShareLowDifficulty, StringComparison.OrdinalIgnoreCase))
                    {
                        TotalBadSharesSubmitted++;
                        await LoggerService.LogMessageAsync($"Low Difficulty Share! [Total = {TotalBadSharesSubmitted}]", ConsoleColor.Red).ConfigureAwait(false);
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