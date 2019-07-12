using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TheDialgaTeam.Xiropht.Xirorig.Mining
{
    public enum ConnectionStatus
    {
        Disconnected,
        Disconnecting,
        Connecting,
        Connected
    }

    public abstract class AbstractListener
    {
        public event Action<AbstractListener> Disconnected;

        public event Action<AbstractListener, bool> LoginResult;

        public event Action<AbstractListener, string> NewJob;

        public event Action<AbstractListener, bool, string> ShareResult;

        public ConnectionStatus ConnectionStatus { get; private set; }

        public bool IsLoggedIn { get; protected set; }

        public int RetryCount { get; protected set; }

        public string Host { get; }

        public ushort Port { get; }

        protected DateTimeOffset LastValidPacketBeforeTimeout { private get; set; }

        private Task CheckNetworkConnectionTask { get; set; }

        private Task ReadPacketFromNetworkTask { get; set; }

        private bool IsActive { get; set; }

        private TcpClient TcpClient { get; set; }

        private StreamReader TcpClientReader { get; set; }

        private StreamWriter TcpClientWriter { get; set; }

        private SemaphoreSlim SemaphoreSlim { get; } = new SemaphoreSlim(1, 1);

        protected AbstractListener(string host, ushort port)
        {
            Host = host;
            Port = port;
        }

        public async Task StartConnectToNetworkAsync()
        {
            if (ConnectionStatus == ConnectionStatus.Connecting || ConnectionStatus == ConnectionStatus.Connected)
                return;

            ConnectionStatus = ConnectionStatus.Connecting;
            IsActive = true;

            if (CheckNetworkConnectionTask == null)
            {
                CheckNetworkConnectionTask = Task.Run(async () =>
                {
                    while (IsActive)
                    {
                        if (ConnectionStatus == ConnectionStatus.Connected && DateTimeOffset.Now > LastValidPacketBeforeTimeout)
                            await DisconnectFromNetworkAsync().ConfigureAwait(false);

                        if (ConnectionStatus == ConnectionStatus.Disconnected)
                            await StartConnectToNetworkAsync().ConfigureAwait(false);

                        await Task.Delay(1).ConfigureAwait(false);
                    }
                });
            }

            try
            {
                TcpClient = new TcpClient();

                var connectTask = TcpClient.ConnectAsync(Host, Port);
                var timeoutTask = Task.Delay(5000);

                await Task.WhenAny(connectTask, timeoutTask).ConfigureAwait(false);

                if (!connectTask.IsCompleted)
                    throw new Exception();

                ConnectionStatus = ConnectionStatus.Connected;

                LastValidPacketBeforeTimeout = DateTimeOffset.Now.AddSeconds(5);

                TcpClientReader = new StreamReader(TcpClient.GetStream());
                TcpClientWriter = new StreamWriter(TcpClient.GetStream());

                ReadPacketFromNetworkTask = Task.Run(async () =>
                {
                    while (ConnectionStatus == ConnectionStatus.Connected)
                    {
                        try
                        {
                            if (TcpClientReader.BaseStream is NetworkStream networkStream)
                            {
                                if (networkStream.DataAvailable)
                                {
                                    var packet = await OnReceivePacketFromNetworkAsync(TcpClientReader).ConfigureAwait(false);
                                    _ = Task.Run(async () => await OnHandlePacketFromNetworkAsync(packet).ConfigureAwait(false));
                                }
                            }

                            await Task.Delay(1).ConfigureAwait(false);
                        }
                        catch (Exception)
                        {
                            await DisconnectFromNetworkAsync().ConfigureAwait(false);
                        }
                    }
                });

                await OnStartConnectToNetworkAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                await DisconnectFromNetworkAsync().ConfigureAwait(false);
            }
        }

        public async Task StopConnectToNetworkAsync()
        {
            if (ConnectionStatus == ConnectionStatus.Disconnecting || ConnectionStatus == ConnectionStatus.Disconnected)
                return;

            ConnectionStatus = ConnectionStatus.Disconnecting;
            IsActive = false;

            if (CheckNetworkConnectionTask != null)
            {
                await CheckNetworkConnectionTask.ConfigureAwait(false);
                CheckNetworkConnectionTask.Dispose();
                CheckNetworkConnectionTask = null;
            }

            TcpClientReader?.Close();
            TcpClientWriter?.Close();
            TcpClient?.Close();

            TcpClientReader?.Dispose();
            TcpClientWriter?.Dispose();
            TcpClient?.Dispose();

            if (ReadPacketFromNetworkTask != null)
            {
                await ReadPacketFromNetworkTask.ConfigureAwait(false);
                ReadPacketFromNetworkTask.Dispose();
                ReadPacketFromNetworkTask = null;
            }

            await OnStopConnectToNetworkAsync().ConfigureAwait(false);

            ConnectionStatus = ConnectionStatus.Disconnected;
            IsLoggedIn = false;
            RetryCount = 0;

            OnDisconnected();
        }

        public async Task SendPacketToNetworkAsync(string packet)
        {
            try
            {
                await SemaphoreSlim.WaitAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                return;
            }

            try
            {
                if (ConnectionStatus == ConnectionStatus.Disconnecting || ConnectionStatus == ConnectionStatus.Disconnected)
                    return;

                await OnSendPacketToNetworkAsync(TcpClientWriter, packet).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await DisconnectFromNetworkAsync().ConfigureAwait(false);
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        protected async Task DisconnectFromNetworkAsync()
        {
            if (ConnectionStatus == ConnectionStatus.Disconnecting || ConnectionStatus == ConnectionStatus.Disconnected)
                return;

            ConnectionStatus = ConnectionStatus.Disconnecting;

            TcpClientReader?.Close();
            TcpClientWriter?.Close();
            TcpClient?.Close();

            TcpClientReader?.Dispose();
            TcpClientWriter?.Dispose();
            TcpClient?.Dispose();

            if (ReadPacketFromNetworkTask != null)
            {
                await ReadPacketFromNetworkTask.ConfigureAwait(false);
                ReadPacketFromNetworkTask.Dispose();
                ReadPacketFromNetworkTask = null;
            }

            await OnDisconnectFromNetworkAsync().ConfigureAwait(false);

            ConnectionStatus = ConnectionStatus.Disconnected;
            IsLoggedIn = false;
            RetryCount++;

            OnDisconnected();
        }

        protected abstract Task OnStartConnectToNetworkAsync();

        protected abstract Task OnStopConnectToNetworkAsync();

        protected abstract Task OnDisconnectFromNetworkAsync();

        protected abstract Task<string> OnReceivePacketFromNetworkAsync(StreamReader reader);

        protected abstract Task OnSendPacketToNetworkAsync(StreamWriter writer, string packet);

        protected abstract Task OnHandlePacketFromNetworkAsync(string packet);

        protected void OnLoginResult(bool success)
        {
            LoginResult?.Invoke(this, success);
        }

        protected void OnNewJob(string packet)
        {
            NewJob?.Invoke(this, packet);
        }

        protected void OnShareResult(bool accepted, string reason)
        {
            ShareResult?.Invoke(this, accepted, reason);
        }

        private void OnDisconnected()
        {
            Disconnected?.Invoke(this);
        }
    }
}