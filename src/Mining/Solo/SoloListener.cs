using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xiropht_Connector_All.RPC;
using Xiropht_Connector_All.Setting;
using Xiropht_Connector_All.SoloMining;
using Xiropht_Connector_All.Utils;

namespace TheDialgaTeam.Xiropht.Xirorig.Mining.Solo
{
    public sealed class SoloListener : AbstractListener
    {
        private string WalletAddress { get; }

        private string ConnectionCertificate { get; }

        private byte[] AesIvCertificate { get; }

        private byte[] AesSaltCertificate { get; }

        public SoloListener(string host, ushort port, string workerId, string walletAddress) : base(host, port, workerId)
        {
            WalletAddress = walletAddress;
            ConnectionCertificate = ClassUtils.GenerateCertificate();

            using (var password = new PasswordDeriveBytes(ConnectionCertificate, ClassUtils.GetByteArrayFromString(ClassUtils.FromHex(ConnectionCertificate.Substring(0, 8)))))
            {
                AesIvCertificate = password.GetBytes(ClassConnectorSetting.MAJOR_UPDATE_1_SECURITY_CERTIFICATE_SIZE / 8);
                AesSaltCertificate = password.GetBytes(16);
            }
        }

        protected override async Task OnStartConnectToNetworkAsync()
        {
            var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5), BaseAddress = new Uri($"http://{Host}:{ClassConnectorSetting.SeedNodeTokenPort}/") };
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Xirorig", Assembly.GetExecutingAssembly().GetName().Version.ToString()));
            var jsonString = await httpClient.GetStringAsync($"{ClassConnectorSettingEnumeration.WalletTokenType}|{ClassRpcWalletCommand.TokenCheckWalletAddressExist}|{WalletAddress}").ConfigureAwait(false);
            var json = JObject.Parse(jsonString);

            if (json.ContainsKey("result"))
            {
                var result = json["result"].ToString();

                if (result.StartsWith(ClassRpcWalletCommand.SendTokenCheckWalletAddressInvalid))
                {
                    await OnLoginResultAsync(false).ConfigureAwait(false);
                    await DisconnectFromNetworkAsync().ConfigureAwait(false);
                    return;
                }
            }

            var packet = new JObject
            {
                { "packet", ConnectionCertificate },
                { "isEncrypted", false }
            };

            await SendPacketToNetworkAsync(packet.ToString(Formatting.None)).ConfigureAwait(false);

            var loginPacket = new JObject
            {
                { "packet", $"{ClassConnectorSettingEnumeration.MinerLoginType}|{WalletAddress}" },
                { "isEncrypted", true }
            };

            await SendPacketToNetworkAsync(loginPacket.ToString(Formatting.None)).ConfigureAwait(false);
        }

        protected override async Task<string> OnReceivePacketFromNetworkAsync(StreamReader reader)
        {
            var buffer = new char[ClassConnectorSetting.MaxNetworkPacketSize];
            var bufferSize = await reader.ReadAsync(buffer, 0, ClassConnectorSetting.MaxNetworkPacketSize).ConfigureAwait(false);
            var encryptedResult = new string(buffer, 0, bufferSize);
            var packets = encryptedResult.Split(new[] { '*' }, StringSplitOptions.RemoveEmptyEntries);
            var packetsLength = packets.Length;
            var decryptedResult = new string[packetsLength];

            for (var i = 0; i < packetsLength; i++)
                decryptedResult[i] = ClassAlgo.GetDecryptedResult(ClassAlgoEnumeration.Rijndael, packets[i], ClassConnectorSetting.MAJOR_UPDATE_1_SECURITY_CERTIFICATE_SIZE, AesIvCertificate, AesSaltCertificate);

            return await Task.FromResult(string.Join("*", decryptedResult));
        }

        protected override async Task OnSendPacketToNetworkAsync(StreamWriter writer, string packet)
        {
            var json = JObject.Parse(packet);

            if (Convert.ToBoolean(json["isEncrypted"].ToString()))
            {
                var packetToSend = $"{ClassAlgo.GetEncryptedResult(ClassAlgoEnumeration.Rijndael, json["packet"].ToString(), ClassConnectorSetting.MAJOR_UPDATE_1_SECURITY_CERTIFICATE_SIZE, AesIvCertificate, AesSaltCertificate)}*";

                if (!packetToSend.StartsWith(ClassAlgoErrorEnumeration.AlgoError))
                {
                    await writer.WriteAsync(packetToSend).ConfigureAwait(false);
                    await writer.FlushAsync().ConfigureAwait(false);
                }
            }
            else
            {
                await writer.WriteAsync(json["packet"].ToString()).ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
            }
        }

        protected override async Task OnHandlePacketFromNetworkAsync(string packet)
        {
            var packets = packet.Split(new[] { '*' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var packetToHandle in packets)
            {
                var packetData = packetToHandle.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                if (packetData[0].Equals(ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.SendLoginAccepted, StringComparison.OrdinalIgnoreCase))
                {
                    LastValidPacketBeforeTimeout = DateTimeOffset.Now.AddSeconds(5);

                    IsLoggedIn = true;
                    RetryCount = 0;

                    await OnLoginResultAsync(true).ConfigureAwait(false);
                }
            }
        }
    }
}