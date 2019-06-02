using System;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TheDialgaTeam.Xiropht.Xirorig.Services.Pool.Packet;

namespace TheDialgaTeam.Xiropht.Xirorig.Services.Pool
{
    public enum ConnectionStatus
    {
        Disconnected,
        Disconnecting,
        Connecting,
        Connected
    }

    public sealed class PoolListener
    {
        public event Func<PoolListener, Task> Disconnected; 

        public event Func<PoolListener, bool, Task> LoginResult;

        public event Func<PoolListener, string, Task> NewJob;

        public event Func<PoolListener, Task> ShareAccepted;

        public event Func<PoolListener, string, Task> ShareRejected;

        public ConnectionStatus ConnectionStatus { get; private set; }

        public bool IsLoggedIn { get; private set; }

        public bool IsActive { get; private set; }

        public string Host { get; }

        public ushort Port { get; }

        public string WalletAddress { get; }

        public string WorkerId { get; }

        private Task CheckConnectionFromPoolNetworkTask { get; set; }

        private Task ReadPacketFromPoolNetworkTask { get; set; }

        private TcpClient PoolClient { get; set; }

        private StreamReader PoolClientReader { get; set; }

        private StreamWriter PoolClientWriter { get; set; }

        private DateTimeOffset LastValidPacketBeforeTimeout { get; set; }

        private SemaphoreSlim AtomicOperation { get; } = new SemaphoreSlim(1, 1);

        public PoolListener(string host, ushort port, string walletAddress, string workerId)
        {
            Host = host;
            Port = port;
            WalletAddress = walletAddress;
            WorkerId = workerId;
        }

        public async Task StartConnectToNetwork()
        {
            if (ConnectionStatus == ConnectionStatus.Connecting || ConnectionStatus == ConnectionStatus.Connected)
                return;

            ConnectionStatus = ConnectionStatus.Connecting;
            IsActive = true;

            try
            {
                PoolClient = new TcpClient();

                var connectTask = PoolClient.ConnectAsync(Host, Port);
                var timeoutTask = Task.Delay(5000);

                await Task.WhenAny(connectTask, timeoutTask).ConfigureAwait(false);

                if (!connectTask.IsCompleted)
                    throw new Exception();

                ConnectionStatus = ConnectionStatus.Connected;

                LastValidPacketBeforeTimeout = DateTimeOffset.Now.AddSeconds(5);

                PoolClientReader = new StreamReader(PoolClient.GetStream());
                PoolClientWriter = new StreamWriter(PoolClient.GetStream());

                if (CheckConnectionFromPoolNetworkTask == null)
                {
                    CheckConnectionFromPoolNetworkTask = Task.Factory.StartNew(async () =>
                    {
                        while (IsActive)
                        {
                            if (DateTimeOffset.Now > LastValidPacketBeforeTimeout)
                                await DisconnectFromNetwork().ConfigureAwait(false);

                            if (ConnectionStatus == ConnectionStatus.Disconnected)
                                await StartConnectToNetwork().ConfigureAwait(false);

                            await Task.Delay(1000).ConfigureAwait(false);
                        }
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Current);
                }

                ReadPacketFromPoolNetworkTask = Task.Factory.StartNew(async () =>
                {
                    while (ConnectionStatus == ConnectionStatus.Connected)
                    {
                        try
                        {
                            var packet = await PoolClientReader.ReadLineAsync().ConfigureAwait(false);

                            if (packet == null)
                                return;

                            _ = Task.Run(async () => await HandlePacketFromPoolNetworkAsync(packet).ConfigureAwait(false));
                        }
                        catch (Exception)
                        {
                            await DisconnectFromNetwork().ConfigureAwait(false);
                        }
                    }
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Current).Unwrap();

                var loginPacket = new JObject
                {
                    { PoolPacket.Type, PoolPacketType.Login },
                    { PoolLoginPacket.WalletAddress, WalletAddress },
                    { PoolLoginPacket.Version, $"Xirorig/{Assembly.GetExecutingAssembly().GetName().Version} {Assembly.GetExecutingAssembly().GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName}" },
                    { PoolLoginPacket.WorkerId, WorkerId }
                };

                await SendPacketToPoolNetworkAsync(loginPacket.ToString(Formatting.None)).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await DisconnectFromNetwork().ConfigureAwait(false);
            }
        }

        public async Task DisconnectFromNetwork()
        {
            if (ConnectionStatus == ConnectionStatus.Disconnecting || ConnectionStatus == ConnectionStatus.Disconnected)
                return;

            ConnectionStatus = ConnectionStatus.Disconnecting;

            PoolClientReader?.Close();
            PoolClientWriter?.Close();
            PoolClient?.Close();

            PoolClientReader?.Dispose();
            PoolClientWriter?.Dispose();
            PoolClient?.Dispose();

            if (ReadPacketFromPoolNetworkTask != null)
            {
                await ReadPacketFromPoolNetworkTask.ConfigureAwait(false);
                ReadPacketFromPoolNetworkTask.Dispose();
            }
            
            ConnectionStatus = ConnectionStatus.Disconnected;
            IsLoggedIn = false;

            if (Disconnected != null)
                await Disconnected(this).ConfigureAwait(false);
        }

        public async Task StopConnectToNetwork()
        {
            if (ConnectionStatus == ConnectionStatus.Disconnecting || ConnectionStatus == ConnectionStatus.Disconnected)
                return;

            ConnectionStatus = ConnectionStatus.Disconnecting;
            IsActive = false;

            if (CheckConnectionFromPoolNetworkTask != null)
            {
                await CheckConnectionFromPoolNetworkTask.ConfigureAwait(false);
                CheckConnectionFromPoolNetworkTask.Dispose();
                CheckConnectionFromPoolNetworkTask = null;
            }

            PoolClientReader?.Close();
            PoolClientWriter?.Close();
            PoolClient?.Close();

            PoolClientReader?.Dispose();
            PoolClientWriter?.Dispose();
            PoolClient?.Dispose();

            if (ReadPacketFromPoolNetworkTask != null)
            {
                await ReadPacketFromPoolNetworkTask.ConfigureAwait(false);
                ReadPacketFromPoolNetworkTask.Dispose();
            }

            ConnectionStatus = ConnectionStatus.Disconnected;
            IsLoggedIn = false;

            if (Disconnected != null)
                await Disconnected(this).ConfigureAwait(false);
        }

        public async Task SendPacketToPoolNetworkAsync(string packet)
        {
            try
            {
                await AtomicOperation.WaitAsync().ConfigureAwait(false);

                if (ConnectionStatus == ConnectionStatus.Disconnecting || ConnectionStatus == ConnectionStatus.Disconnected)
                    return;

                await PoolClientWriter.WriteLineAsync(packet).ConfigureAwait(false);
                await PoolClientWriter.FlushAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                await DisconnectFromNetwork().ConfigureAwait(false);
            }
            finally
            {
                AtomicOperation.Release();
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
                        if (LoginResult != null)
                            await LoginResult(this, false).ConfigureAwait(false);
                        
                        await StopConnectToNetwork().ConfigureAwait(false);
                    }

                    if (jsonPacket.ContainsKey(PoolLoginPacket.LoginOkay))
                    {
                        IsLoggedIn = true;

                        if (LoginResult != null)
                            await LoginResult(this, true).ConfigureAwait(false);
                    }
                }

                if (string.Equals(type, PoolPacketType.KeepAlive, StringComparison.OrdinalIgnoreCase))
                    LastValidPacketBeforeTimeout = DateTimeOffset.Now.AddSeconds(5);

                if (string.Equals(type, PoolPacketType.Job, StringComparison.OrdinalIgnoreCase))
                {
                    LastValidPacketBeforeTimeout = DateTimeOffset.Now.AddSeconds(5);

                    if (NewJob != null)
                        await NewJob(this, packet).ConfigureAwait(false);
                }

                if (string.Equals(type, PoolPacketType.Share, StringComparison.OrdinalIgnoreCase))
                {
                    LastValidPacketBeforeTimeout = DateTimeOffset.Now.AddSeconds(5);

                    var result = jsonPacket[PoolSharePacket.Result].ToString();

                    if (string.Equals(result, PoolSharePacket.ResultShareOk, StringComparison.OrdinalIgnoreCase))
                    {
                        if (ShareAccepted != null)
                            await ShareAccepted(this).ConfigureAwait(false);
                    }

                    if (string.Equals(result, PoolSharePacket.ResultShareInvalid, StringComparison.OrdinalIgnoreCase))
                    {
                        if (ShareRejected != null)
                            await ShareRejected(this, "Invalid Share.").ConfigureAwait(false);
                    }

                    if (string.Equals(result, PoolSharePacket.ResultShareDuplicate, StringComparison.OrdinalIgnoreCase))
                    {
                        if (ShareRejected != null)
                            await ShareRejected(this, "Duplicate Share.").ConfigureAwait(false);
                    }

                    if (string.Equals(result, PoolSharePacket.ResultShareLowDifficulty, StringComparison.OrdinalIgnoreCase))
                    {
                        if (ShareRejected != null)
                            await ShareRejected(this, "Low Share Difficulty.").ConfigureAwait(false);
                    }
                }
            }
            catch (Exception)
            {
                // Invalid packets received. Maybe ignore this?
            }
        }
    }
}