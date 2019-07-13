using System;
using System.Diagnostics;
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

        private JObject CurrentWorkingBlockTemplate { get; set; }

        private JObject CurrentBlockTemplate { get; set; }

        private Stopwatch Stopwatch { get; }

        private bool IsWalletAddressValid { get; set; }

        public SoloListener(string host, ushort port, string walletAddress) : base(host, port)
        {
            WalletAddress = walletAddress;
            ConnectionCertificate = ClassUtils.GenerateCertificate();
            Stopwatch = new Stopwatch();
            Stopwatch.Stop();

            using (var password = new PasswordDeriveBytes(ConnectionCertificate, ClassUtils.GetByteArrayFromString(ClassUtils.FromHex(ConnectionCertificate.Substring(0, 8)))))
            {
                AesIvCertificate = password.GetBytes(ClassConnectorSetting.MAJOR_UPDATE_1_SECURITY_CERTIFICATE_SIZE / 8);
                AesSaltCertificate = password.GetBytes(16);
            }
        }

        protected override async Task OnStartConnectToNetworkAsync()
        {
            if (!IsWalletAddressValid)
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
                        OnLoginResult(false);
                        await DisconnectFromNetworkAsync().ConfigureAwait(false);
                        return;
                    }
                }

                IsWalletAddressValid = true;
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

                    OnLoginResult(true);

                    var askCurrentBlockTemplate = new JObject
                    {
                        { "packet", ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveAskCurrentBlockMining },
                        { "isEncrypted", true }
                    };

                    await SendPacketToNetworkAsync(askCurrentBlockTemplate.ToString(Formatting.None)).ConfigureAwait(false);
                }
                else if (packetData[0].Equals(ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.SendCurrentBlockMining, StringComparison.OrdinalIgnoreCase))
                {
                    LastValidPacketBeforeTimeout = DateTimeOffset.Now.AddSeconds(5);
                    Stopwatch.Restart();

                    var currentBlockData = packetData[1].Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);

                    CurrentBlockTemplate = new JObject();

                    foreach (var data in currentBlockData)
                    {
                        var keyValuePair = data.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                        CurrentBlockTemplate.Add(keyValuePair[0], keyValuePair[1]);
                    }

                    if (CurrentBlockTemplate.ContainsKey("METHOD"))
                    {
                        var askBlockContent = new JObject
                        {
                            { "packet", $"{ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveAskContentBlockMethod}|{CurrentBlockTemplate["METHOD"]}" },
                            { "isEncrypted", true }
                        };

                        await SendPacketToNetworkAsync(askBlockContent.ToString(Formatting.None)).ConfigureAwait(false);
                    }
                }
                else if (packetData[0].Equals(ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.SendContentBlockMethod, StringComparison.OrdinalIgnoreCase))
                {
                    LastValidPacketBeforeTimeout = DateTimeOffset.Now.AddSeconds(5);

                    var currentBlockData = packetData[1].Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries);

                    CurrentBlockTemplate.Add("AESROUND", currentBlockData[0]);
                    CurrentBlockTemplate.Add("AESSIZE", currentBlockData[1]);
                    CurrentBlockTemplate.Add("AESKEY", currentBlockData[2]);
                    CurrentBlockTemplate.Add("XORKEY", currentBlockData[3]);

                    if (!string.IsNullOrWhiteSpace(CurrentBlockTemplate["INDICATION"].ToString()))
                    {
                        if (CurrentWorkingBlockTemplate == null)
                        {
                            CurrentWorkingBlockTemplate = CurrentBlockTemplate;
                            OnNewJob(CurrentWorkingBlockTemplate.ToString(Formatting.None));
                        }
                        else if (CurrentWorkingBlockTemplate["INDICATION"].ToString() != CurrentBlockTemplate["INDICATION"].ToString())
                        {
                            CurrentWorkingBlockTemplate = CurrentBlockTemplate;
                            OnNewJob(CurrentWorkingBlockTemplate.ToString(Formatting.None));
                        }
                    }

                    while (IsLoggedIn && Stopwatch.ElapsedMilliseconds < 1000)
                        await Task.Delay(1).ConfigureAwait(false);

                    var askCurrentBlockTemplate = new JObject
                    {
                        { "packet", ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveAskCurrentBlockMining },
                        { "isEncrypted", true }
                    };

                    await SendPacketToNetworkAsync(askCurrentBlockTemplate.ToString(Formatting.None)).ConfigureAwait(false);
                }
                else if (packetData[0].Equals(ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.SendJobStatus, StringComparison.OrdinalIgnoreCase))
                {
                    LastValidPacketBeforeTimeout = DateTimeOffset.Now.AddSeconds(5);

                    if (packetData[1].Equals(ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.ShareUnlock))
                        OnShareResult(true, "Share Accepted.");
                    else if (packetData[1].Equals(ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.ShareWrong))
                        OnShareResult(false, "Invalid Share.");
                    else if (packetData[1].Equals(ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.ShareAleady))
                        OnShareResult(false, "Orphan Share.");
                    else if (packetData[1].Equals(ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.ShareNotExist))
                        OnShareResult(false, "Invalid Share.");
                    else if (packetData[1].Equals(ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.ShareGood))
                        OnShareResult(true, "Share Accepted.");
                    else if (packetData[1].Equals(ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.ShareBad))
                        OnShareResult(false, "Invalid/Orphan Share.");
                }
            }
        }
    }
}