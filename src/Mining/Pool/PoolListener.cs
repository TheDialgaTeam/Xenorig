using System;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TheDialgaTeam.Xiropht.Xirorig.Mining.Pool.Packet;

namespace TheDialgaTeam.Xiropht.Xirorig.Mining.Pool
{
    public sealed class PoolListener : AbstractListener
    {
        private string WalletAddress { get; }

        public PoolListener(string host, ushort port, string workerId, string walletAddress) : base(host, port, workerId)
        {
            WalletAddress = walletAddress;
        }

        protected override async Task OnStartConnectToNetworkAsync()
        {
            var loginPacket = new JObject
            {
                { PoolPacket.Type, PoolPacketType.Login },
                { PoolLoginPacket.WalletAddress, WalletAddress },
                { PoolLoginPacket.Version, $"Xirorig/{Assembly.GetExecutingAssembly().GetName().Version} {Assembly.GetExecutingAssembly().GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName}" },
                { PoolLoginPacket.WorkerId, WorkerId }
            };

            await SendPacketToNetworkAsync(loginPacket.ToString(Formatting.None)).ConfigureAwait(false);
        }

        protected override Task OnStopConnectToNetworkAsync()
        {
            return Task.CompletedTask;
        }

        protected override Task OnDisconnectFromNetworkAsync()
        {
            return Task.CompletedTask;
        }

        protected override async Task<string> OnReceivePacketFromNetworkAsync(StreamReader reader)
        {
            return await reader.ReadLineAsync().ConfigureAwait(false);
        }

        protected override async Task OnSendPacketToNetworkAsync(StreamWriter writer, string packet)
        {
            await writer.WriteLineAsync(packet).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
        }

        protected override async Task OnHandlePacketFromNetworkAsync(string packet)
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
                else if (string.Equals(type, PoolPacketType.KeepAlive, StringComparison.OrdinalIgnoreCase))
                    LastValidPacketBeforeTimeout = DateTimeOffset.Now.AddSeconds(5);
                else if (string.Equals(type, PoolPacketType.Job, StringComparison.OrdinalIgnoreCase))
                {
                    LastValidPacketBeforeTimeout = DateTimeOffset.Now.AddSeconds(5);

                    await OnNewJobAsync(packet).ConfigureAwait(false);
                }
                else if (string.Equals(type, PoolPacketType.Share, StringComparison.OrdinalIgnoreCase))
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