using System;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TheDialgaTeam.Xiropht.Xirorig.Mining.Pool.Packet;

namespace TheDialgaTeam.Xiropht.Xirorig.Mining.Pool
{
    public sealed class PoolListener : AbstractListener
    {
        private string WalletAddress { get; }

        private string WorkerId { get; }

        private Task CheckConnectionFromPoolNetworkTask { get; set; }

        private Task ReadPacketFromPoolNetworkTask { get; set; }

        private TcpClient PoolClient { get; set; }

        private StreamReader PoolClientReader { get; set; }

        private StreamWriter PoolClientWriter { get; set; }

        private DateTimeOffset LastValidPacketBeforeTimeout { get; set; }

        private SemaphoreSlim AtomicOperation { get; } = new SemaphoreSlim(1, 1);

        public PoolListener(string host, ushort port, string walletAddress, string workerId) : base(host, port)
        {
            WalletAddress = walletAddress;
            WorkerId = workerId;
        }

        public override async Task StartConnectToNetworkAsync()
        {
            if (ConnectionStatus == ConnectionStatus.Connecting || ConnectionStatus == ConnectionStatus.Connected)
                return;

            ConnectionStatus = ConnectionStatus.Connecting;
            IsActive = true;

            if (CheckConnectionFromPoolNetworkTask == null)
            {
                CheckConnectionFromPoolNetworkTask = Task.Factory.StartNew(async () =>
                {
                    while (IsActive)
                    {
                        if (ConnectionStatus == ConnectionStatus.Connected && DateTimeOffset.Now > LastValidPacketBeforeTimeout)
                            await DisconnectFromNetworkAsync().ConfigureAwait(false);

                        if (ConnectionStatus == ConnectionStatus.Disconnected)
                            await StartConnectToNetworkAsync().ConfigureAwait(false);

                        await Task.Delay(1000).ConfigureAwait(false);
                    }
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Current).Unwrap();
            }

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

                ReadPacketFromPoolNetworkTask = Task.Factory.StartNew(async () =>
                {
                    while (ConnectionStatus == ConnectionStatus.Connected)
                    {
                        try
                        {
                            if (PoolClientReader.BaseStream is NetworkStream networkStream)
                            {
                                if (networkStream.DataAvailable)
                                {
                                    var packet = await PoolClientReader.ReadLineAsync().ConfigureAwait(false);
                                    _ = Task.Run(async () => await HandlePacketFromPoolNetworkAsync(packet).ConfigureAwait(false));
                                }
                            }

                            await Task.Delay(1).ConfigureAwait(false);
                        }
                        catch (Exception)
                        {
                            await DisconnectFromNetworkAsync().ConfigureAwait(false);
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
                await DisconnectFromNetworkAsync().ConfigureAwait(false);
            }
        }

        public override async Task StopConnectToNetworkAsync()
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
                ReadPacketFromPoolNetworkTask = null;
            }

            ConnectionStatus = ConnectionStatus.Disconnected;
            IsLoggedIn = false;
            RetryCount = 0;

            await OnDisconnectedAsync().ConfigureAwait(false);
        }

        public override async Task SendPacketToPoolNetworkAsync(string packet)
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
                await DisconnectFromNetworkAsync().ConfigureAwait(false);
            }
            finally
            {
                AtomicOperation.Release();
            }
        }

        private async Task DisconnectFromNetworkAsync()
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
                ReadPacketFromPoolNetworkTask = null;
            }

            ConnectionStatus = ConnectionStatus.Disconnected;
            IsLoggedIn = false;
            RetryCount++;

            await OnDisconnectedAsync().ConfigureAwait(false);
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
                        await OnLoginResultAsync(false).ConfigureAwait(false);
                        await StopConnectToNetworkAsync().ConfigureAwait(false);
                    }

                    if (jsonPacket.ContainsKey(PoolLoginPacket.LoginOkay))
                    {
                        IsLoggedIn = true;
                        RetryCount = 0;

                        await OnLoginResultAsync(true).ConfigureAwait(false);
                    }
                }

                if (string.Equals(type, PoolPacketType.KeepAlive, StringComparison.OrdinalIgnoreCase))
                    LastValidPacketBeforeTimeout = DateTimeOffset.Now.AddSeconds(5);

                if (string.Equals(type, PoolPacketType.Job, StringComparison.OrdinalIgnoreCase))
                {
                    LastValidPacketBeforeTimeout = DateTimeOffset.Now.AddSeconds(5);

                    await OnNewJobAsync(packet).ConfigureAwait(false);
                }

                if (string.Equals(type, PoolPacketType.Share, StringComparison.OrdinalIgnoreCase))
                {
                    LastValidPacketBeforeTimeout = DateTimeOffset.Now.AddSeconds(5);

                    var result = jsonPacket[PoolSharePacket.Result].ToString();

                    if (string.Equals(result, PoolSharePacket.ResultShareOk, StringComparison.OrdinalIgnoreCase))
                        await OnShareResultAsync(true, "Share Accepted.").ConfigureAwait(false);

                    if (string.Equals(result, PoolSharePacket.ResultShareInvalid, StringComparison.OrdinalIgnoreCase))
                        await OnShareResultAsync(false, "Invalid Share.").ConfigureAwait(false);

                    if (string.Equals(result, PoolSharePacket.ResultShareDuplicate, StringComparison.OrdinalIgnoreCase))
                        await OnShareResultAsync(false, "Duplicate Share.").ConfigureAwait(false);

                    if (string.Equals(result, PoolSharePacket.ResultShareLowDifficulty, StringComparison.OrdinalIgnoreCase))
                        await OnShareResultAsync(false, "Low Share Difficulty.").ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                // Invalid packets received. Maybe ignore this?
            }
        }
    }
}