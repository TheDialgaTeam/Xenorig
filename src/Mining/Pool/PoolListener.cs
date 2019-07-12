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

        private string WorkerId { get; }

        public PoolListener(string host, ushort port, string walletAddress, string workerId) : base(host, port)
        {
            WalletAddress = walletAddress;
            WorkerId = workerId;
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
                        OnLoginResult(false);
                        await StopConnectToNetworkAsync().ConfigureAwait(false);
                    }

                    if (jsonPacket.ContainsKey(PoolLoginPacket.LoginOkay))
                    {
                        IsLoggedIn = true;
                        RetryCount = 0;

                        OnLoginResult(true);
                    }
                }
                else if (string.Equals(type, PoolPacketType.KeepAlive, StringComparison.OrdinalIgnoreCase))
                    LastValidPacketBeforeTimeout = DateTimeOffset.Now.AddSeconds(5);
                else if (string.Equals(type, PoolPacketType.Job, StringComparison.OrdinalIgnoreCase))
                {
                    LastValidPacketBeforeTimeout = DateTimeOffset.Now.AddSeconds(5);

                    OnNewJob(packet);
                }
                else if (string.Equals(type, PoolPacketType.Share, StringComparison.OrdinalIgnoreCase))
                {
                    LastValidPacketBeforeTimeout = DateTimeOffset.Now.AddSeconds(5);

                    var result = jsonPacket[PoolSharePacket.Result].ToString();

                    if (string.Equals(result, PoolSharePacket.ResultShareOk, StringComparison.OrdinalIgnoreCase))
                        OnShareResult(true, "Share Accepted.");

                    if (string.Equals(result, PoolSharePacket.ResultShareInvalid, StringComparison.OrdinalIgnoreCase))
                        OnShareResult(false, "Invalid Share.");

                    if (string.Equals(result, PoolSharePacket.ResultShareDuplicate, StringComparison.OrdinalIgnoreCase))
                        OnShareResult(false, "Duplicate Share.");

                    if (string.Equals(result, PoolSharePacket.ResultShareLowDifficulty, StringComparison.OrdinalIgnoreCase))
                        OnShareResult(false, "Low Share Difficulty.");
                }
            }
            catch (Exception)
            {
                // Invalid packets received. Maybe ignore this?
            }
        }
    }
}