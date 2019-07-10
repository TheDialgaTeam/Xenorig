using System;
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
        public event Func<AbstractListener, Task> Disconnected;

        public event Func<AbstractListener, bool, Task> LoginResult;

        public event Func<AbstractListener, string, Task> NewJob;

        public event Func<AbstractListener, bool, string, Task> ShareResult;

        public ConnectionStatus ConnectionStatus { get; protected set; }

        public bool IsLoggedIn { get; protected set; }

        public bool IsActive { get; protected set; }

        public int RetryCount { get; protected set; }

        public string Host { get; }

        public ushort Port { get; }

        protected AbstractListener(string host, ushort port)
        {
            Host = host;
            Port = port;
        }

        public abstract Task StartConnectToNetworkAsync();

        public abstract Task StopConnectToNetworkAsync();

        public abstract Task SendPacketToPoolNetworkAsync(string packet);

        protected async Task OnDisconnectedAsync()
        {
            if (Disconnected != null)
                await Disconnected(this).ConfigureAwait(false);
        }

        protected async Task OnLoginResultAsync(bool success)
        {
            if (LoginResult != null)
                await LoginResult(this, success).ConfigureAwait(false);
        }

        protected async Task OnNewJobAsync(string packet)
        {
            if (NewJob != null)
                await NewJob(this, packet).ConfigureAwait(false);
        }

        protected async Task OnShareResultAsync(bool accepted, string reason)
        {
            if (ShareResult != null)
                await ShareResult(this, accepted, reason).ConfigureAwait(false);
        }
    }
}