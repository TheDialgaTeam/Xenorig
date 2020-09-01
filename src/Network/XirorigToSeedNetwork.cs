using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using TheDialgaTeam.Xiropht.Xirorig.Config;
using Xiropht_Connector_All.RPC;
using Xiropht_Connector_All.Setting;
using Xiropht_Connector_All.SoloMining;
using Xiropht_Connector_All.Utils;

namespace TheDialgaTeam.Xiropht.Xirorig.Network
{
    public class XirorigToSeedNetwork : IDisposable
    {
        public event Action<string>? Disconnected;
        public event Action<string, bool>? LoginResult;
        public event Action<string, JObject>? NewJob;
        public event Action<bool, string, string>? BlockResult;

        private readonly XirorigConfiguration _xirorigConfiguration;
        private readonly ILogger<XirorigToSeedNetwork> _logger;

        private string[]? _seedNodeIpAddresses;
        private int _seedNodeIpAddressIndex;
        private int _seedNodeIpAddressRetryCount;

        private bool _isActive;
        private ConnectionStatus _connectionStatus = ConnectionStatus.Disconnected;
        private DateTimeOffset _lastValidPacketDateTimeOffset = DateTimeOffset.Now;
        private CancellationTokenSource? _cancellationTokenSource;
        private CancellationTokenSource? _globalCancellationTokenSource;

        private TcpClient? _tcpClient;
        private StreamReader? _tcpClientReader;
        private StreamWriter? _tcpClientWriter;

        private readonly string _connectionCertificate;
        private bool _isWalletAddressValid;

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        private readonly Aes _aes;
        private readonly ICryptoTransform _aesEncryptor;
        private readonly ICryptoTransform _aesDecryptor;

        private JObject? _currentBlockTemplate;
        private JObject? _currentWorkingBlockTemplate;

        private Task? _checkConnectionNetworkTask;
        private Task? _readPacketFromNetworkTask;

        public XirorigToSeedNetwork(XirorigConfiguration xirorigConfiguration, ILogger<XirorigToSeedNetwork> logger)
        {
            _xirorigConfiguration = xirorigConfiguration;
            _logger = logger;

            _connectionCertificate = ClassUtils.GenerateCertificate();

            using var password = new PasswordDeriveBytes(_connectionCertificate, Encoding.UTF8.GetBytes(ClassUtils.FromHex(_connectionCertificate.Substring(0, 8))));
#pragma warning disable 618
            var aesIvCertificate = password.GetBytes(ClassConnectorSetting.MAJOR_UPDATE_1_SECURITY_CERTIFICATE_SIZE / 8);
            var aesSaltCertificate = password.GetBytes(16);
#pragma warning restore 618

            _aes = Aes.Create();
            _aes.Mode = CipherMode.CFB;
            _aes.BlockSize = 128;
            _aes.Padding = PaddingMode.None;
            _aes.KeySize = ClassConnectorSetting.MAJOR_UPDATE_1_SECURITY_CERTIFICATE_SIZE;
            _aes.Key = aesIvCertificate;

            _aesEncryptor = _aes.CreateEncryptor(aesIvCertificate, aesSaltCertificate);
            _aesDecryptor = _aes.CreateDecryptor(aesIvCertificate, aesSaltCertificate);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_isActive) return;

            _cancellationTokenSource?.Dispose();
            _globalCancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            _globalCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

            await GetSeedNodeHostAsync().ConfigureAwait(false);
            await StartConnectingToSeedNetworkAsync().ConfigureAwait(false);
        }

        public async Task StopAsync()
        {
            if (!_isActive) return;
            _isActive = false;

            _cancellationTokenSource!.Cancel();
            _cancellationTokenSource!.Dispose();
            _globalCancellationTokenSource!.Dispose();

            if (_checkConnectionNetworkTask != null)
            {
                await _checkConnectionNetworkTask.ConfigureAwait(false);
                _checkConnectionNetworkTask.Dispose();
                _checkConnectionNetworkTask = null;
            }

            if (_readPacketFromNetworkTask != null)
            {
                await _readPacketFromNetworkTask.ConfigureAwait(false);
                _readPacketFromNetworkTask.Dispose();
                _readPacketFromNetworkTask = null;
            }
        }

        public async Task SendPacketToNetworkAsync(string packet, bool isEncrypted)
        {
            try
            {
                await _semaphoreSlim.WaitAsync(_globalCancellationTokenSource!.Token).ConfigureAwait(false);
            }
            catch (Exception)
            {
                return;
            }

            try
            {
                if (_connectionStatus == ConnectionStatus.Disconnecting || _connectionStatus == ConnectionStatus.Disconnected) return;

                if (isEncrypted)
                {
                    var packetBytes = Encoding.UTF8.GetBytes(packet);
                    var packetLength = packetBytes.Length;

                    var paddingBlockSize = _aes.BlockSize / 8;
                    var paddingSizeRequired = paddingBlockSize - packetLength % paddingBlockSize;
                    var paddedBytes = ArrayPool<byte>.Shared.Rent(packetLength + paddingSizeRequired);

                    try
                    {
                        Buffer.BlockCopy(packetBytes, 0, paddedBytes, 0, packetLength);

                        for (var i = 0; i < paddingSizeRequired; i++)
                        {
                            paddedBytes[packetLength + i] = (byte) paddingSizeRequired;
                        }

                        var encryptedPacketBytes = _aesEncryptor.TransformFinalBlock(paddedBytes, 0, packetLength + paddingSizeRequired);

                        await _tcpClientWriter!.WriteAsync($"{Convert.ToBase64String(encryptedPacketBytes)}*").ConfigureAwait(false);
                        await _tcpClientWriter.FlushAsync().ConfigureAwait(false);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(paddedBytes);
                    }
                }
                else
                {
                    await _tcpClientWriter!.WriteAsync(packet).ConfigureAwait(false);
                    await _tcpClientWriter.FlushAsync().ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                await DisconnectFromSeedNetworkAsync().ConfigureAwait(false);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private async Task GetSeedNodeHostAsync()
        {
            var seedNodeIpAddressesPing = new Dictionary<string, long>();

            using (var ping = new Ping())
            {
                foreach (var seedNodeIpAddress in _xirorigConfiguration.SeedNodeIpAddresses)
                {
                    try
                    {
                        var result = await ping.SendPingAsync(seedNodeIpAddress).ConfigureAwait(false);

                        if (result.Status == IPStatus.Success)
                        {
                            seedNodeIpAddressesPing.Add(seedNodeIpAddress, result.RoundtripTime);
                        }
                    }
                    catch (PingException)
                    {
                        seedNodeIpAddressesPing.Add(seedNodeIpAddress, -1);
                    }
                }
            }

            var sortedSeedNodeIpAddressesPing = seedNodeIpAddressesPing.OrderBy(a => a.Value);
            var seedNodeIpAddresses = new List<string>();
            var index = 0;

            foreach (var (ipAddress, ping) in sortedSeedNodeIpAddressesPing)
            {
                seedNodeIpAddresses.Add(ipAddress);
                _logger.LogInformation(" \u001b[32;1m*\u001b[0m SOLO #{index,-7:l}\u001b[36;1m{nodeIp:l}\u001b[0m ({nodePing}ms)", (++index).ToString(), ipAddress, ping);
            }

            _seedNodeIpAddresses = seedNodeIpAddresses.ToArray();
        }

        private async Task StartConnectingToSeedNetworkAsync()
        {
            if (_connectionStatus == ConnectionStatus.Connecting || _connectionStatus == ConnectionStatus.Connected) return;

            _connectionStatus = ConnectionStatus.Connecting;
            _isActive = true;

            StartCheckingNetworkConnection();

            if (++_seedNodeIpAddressRetryCount > 5 && ++_seedNodeIpAddressIndex > _seedNodeIpAddresses!.Length)
            {
                _seedNodeIpAddressIndex = 0;
            }

            try
            {
                _tcpClient = new TcpClient();

                using (var connectTimeoutCancellationTokenSource = new CancellationTokenSource(5000))
                {
                    using (var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_globalCancellationTokenSource!.Token, connectTimeoutCancellationTokenSource.Token))
                    {
                        await _tcpClient.ConnectAsync(IPAddress.Parse(_seedNodeIpAddresses![_seedNodeIpAddressIndex]), _xirorigConfiguration.SeedNodePort, linkedCancellationTokenSource.Token).ConfigureAwait(false);
                    }
                }

                _tcpClientReader = new StreamReader(_tcpClient.GetStream());
                _tcpClientWriter = new StreamWriter(_tcpClient.GetStream());

                if (!_isWalletAddressValid)
                {
                    var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5), BaseAddress = new Uri($"http://{_seedNodeIpAddresses[_seedNodeIpAddressIndex]}:{_xirorigConfiguration.SeedNodeTokenPort}/") };
                    httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Xirorig", Assembly.GetExecutingAssembly().GetName().Version?.ToString()));

                    var jsonString = await httpClient.GetStringAsync($"{ClassConnectorSettingEnumeration.WalletTokenType}|{ClassRpcWalletCommand.TokenCheckWalletAddressExist}|{_xirorigConfiguration.WalletAddress}", _globalCancellationTokenSource!.Token).ConfigureAwait(false);
                    var json = JObject.Parse(jsonString);

                    if (json.TryGetValue("result", StringComparison.OrdinalIgnoreCase, out var resultToken))
                    {
                        var result = resultToken.Value<string>();

                        if (result.StartsWith(ClassRpcWalletCommand.SendTokenCheckWalletAddressInvalid))
                        {
                            LoginResult?.Invoke($"{_seedNodeIpAddresses![_seedNodeIpAddressIndex]}:{_xirorigConfiguration.SeedNodePort}", false);
                            await DisconnectFromSeedNetworkAsync().ConfigureAwait(false);
                            return;
                        }
                    }

                    _isWalletAddressValid = true;
                }

                _connectionStatus = ConnectionStatus.Connected;
                StartReadingPacketFromNetwork();

                await SendPacketToNetworkAsync(_connectionCertificate, false).ConfigureAwait(false);
                await SendPacketToNetworkAsync($"{ClassConnectorSettingEnumeration.MinerLoginType}|{_xirorigConfiguration.WalletAddress}", true).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await DisconnectFromSeedNetworkAsync().ConfigureAwait(false);
            }
        }

        private async Task DisconnectFromSeedNetworkAsync()
        {
            if (_connectionStatus == ConnectionStatus.Disconnecting || _connectionStatus == ConnectionStatus.Disconnected) return;

            _connectionStatus = ConnectionStatus.Disconnecting;

            _tcpClientReader?.Dispose();

            if (_tcpClientWriter != null)
            {
                await _tcpClientWriter.DisposeAsync().ConfigureAwait(false);
            }

            _tcpClient?.Dispose();

            _connectionStatus = ConnectionStatus.Disconnected;
            Disconnected?.Invoke($"{_seedNodeIpAddresses![_seedNodeIpAddressIndex]}:{_xirorigConfiguration.SeedNodePort}");
        }

        private void StartCheckingNetworkConnection()
        {
            if (_checkConnectionNetworkTask != null) return;

            _checkConnectionNetworkTask = Task.Factory.StartNew(async state =>
            {
                if (state is (XirorigToSeedNetwork xirorigToSeedNetwork, CancellationToken cancellationTokenState))
                {
                    while (xirorigToSeedNetwork._isActive)
                    {
                        switch (xirorigToSeedNetwork._connectionStatus)
                        {
                            case ConnectionStatus.Connected when DateTimeOffset.Now - xirorigToSeedNetwork._lastValidPacketDateTimeOffset > TimeSpan.FromSeconds(5000):
                                await xirorigToSeedNetwork.DisconnectFromSeedNetworkAsync().ConfigureAwait(false);
                                break;

                            case ConnectionStatus.Disconnected:
                                await xirorigToSeedNetwork.StartConnectingToSeedNetworkAsync().ConfigureAwait(false);
                                break;
                        }

                        await Task.Delay(1000, cancellationTokenState).ConfigureAwait(false);
                    }
                }
            }, (this, _globalCancellationTokenSource!.Token), _globalCancellationTokenSource!.Token, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap();
        }

        private void StartReadingPacketFromNetwork()
        {
            _lastValidPacketDateTimeOffset = DateTimeOffset.Now;

            _readPacketFromNetworkTask = Task.Factory.StartNew(async state =>
            {
                if (state is (XirorigToSeedNetwork xirorigToSeedNetwork, CancellationToken cancellationToken))
                {
                    var buffer = ArrayPool<char>.Shared.Rent(ClassConnectorSetting.MaxNetworkPacketSize);
                    var tcpClientReader = xirorigToSeedNetwork._tcpClientReader;

                    while (xirorigToSeedNetwork._connectionStatus == ConnectionStatus.Connected)
                    {
                        try
                        {
                            if (!(tcpClientReader!.BaseStream is NetworkStream networkStream)) continue;

                            if (networkStream.DataAvailable)
                            {
                                var bufferSize = await tcpClientReader!.ReadAsync(buffer, 0, ClassConnectorSetting.MaxNetworkPacketSize).ConfigureAwait(false);
                                var decryptedPackets = GetDecryptedPackets(buffer.AsSpan(), bufferSize);

                                _ = Task.Factory.StartNew(async innerState =>
                                {
                                    if (innerState is string[] decryptedPacketsState)
                                    {
                                        await HandlePacketFromNetworkAsync(decryptedPacketsState).ConfigureAwait(false);
                                    }
                                }, decryptedPackets, cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                            }

                            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception)
                        {
                            if (cancellationToken.IsCancellationRequested) return;
                            await xirorigToSeedNetwork.DisconnectFromSeedNetworkAsync().ConfigureAwait(false);
                        }
                    }

                    ArrayPool<char>.Shared.Return(buffer);
                }
            }, (this, _globalCancellationTokenSource!.Token), _globalCancellationTokenSource!.Token, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap();
        }

        private string[] GetDecryptedPackets(Span<char> buffer, int bufferSize)
        {
            var result = new List<string>();
            var currentBuffer = buffer.Slice(0, bufferSize);

            do
            {
                var indexToSlice = currentBuffer.IndexOf('*');
                var packet = currentBuffer.Slice(0, indexToSlice);
                var packetBytes = Convert.FromBase64CharArray(packet.ToArray(), 0, packet.Length);

                var decryptedPaddedBytes = _aesDecryptor.TransformFinalBlock(packetBytes, 0, packetBytes.Length).AsSpan();
                var decryptedBytes = decryptedPaddedBytes.Slice(0, decryptedPaddedBytes.Length - decryptedPaddedBytes[^1]);

                result.Add(Encoding.UTF8.GetString(decryptedBytes));

                currentBuffer = currentBuffer.Slice(indexToSlice + 1);
            } while (currentBuffer.Length > 0);

            return result.ToArray();
        }

        private async Task HandlePacketFromNetworkAsync(string[] packets)
        {
            foreach (var packet in packets)
            {
                if (packet.StartsWith(ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.SendLoginAccepted))
                {
                    _lastValidPacketDateTimeOffset = DateTimeOffset.Now;
                    _seedNodeIpAddressRetryCount = 0;

                    LoginResult?.Invoke($"{_seedNodeIpAddresses![_seedNodeIpAddressIndex]}:{_xirorigConfiguration.SeedNodePort}", true);

                    await SendPacketToNetworkAsync(ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveAskCurrentBlockMining, true).ConfigureAwait(false);
                }
                else if (packet.StartsWith(ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.SendCurrentBlockMining))
                {
                    _lastValidPacketDateTimeOffset = DateTimeOffset.Now;

                    var currentBlockData = packet.Substring(packet.IndexOf('|') + 1).Split('&', StringSplitOptions.RemoveEmptyEntries);

                    _currentBlockTemplate = new JObject();

                    foreach (var data in currentBlockData)
                    {
                        var splitData = data.Split('=', StringSplitOptions.RemoveEmptyEntries);
                        _currentBlockTemplate.Add(splitData[0], splitData[1]);
                    }

                    if (_currentBlockTemplate.ContainsKey("METHOD"))
                    {
                        await SendPacketToNetworkAsync($"{ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveAskContentBlockMethod}|{_currentBlockTemplate["METHOD"]}", true).ConfigureAwait(false);
                    }
                }
                else if (packet.StartsWith(ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.SendContentBlockMethod))
                {
                    _lastValidPacketDateTimeOffset = DateTimeOffset.Now;

                    var currentBlockData = packet.Substring(packet.IndexOf('|') + 1).Split('#', StringSplitOptions.RemoveEmptyEntries);

                    _currentBlockTemplate!.Add("AESROUND", currentBlockData[0]);
                    _currentBlockTemplate.Add("AESSIZE", currentBlockData[1]);
                    _currentBlockTemplate.Add("AESKEY", currentBlockData[2]);
                    _currentBlockTemplate.Add("XORKEY", currentBlockData[3]);

                    if (!string.IsNullOrWhiteSpace(_currentBlockTemplate["INDICATION"]!.Value<string>()))
                    {
                        if (_currentWorkingBlockTemplate == null || _currentWorkingBlockTemplate["INDICATION"]!.Value<string>() != _currentBlockTemplate["INDICATION"]!.Value<string>())
                        {
                            _currentWorkingBlockTemplate = _currentBlockTemplate;
                            NewJob?.Invoke($"{_seedNodeIpAddresses![_seedNodeIpAddressIndex]}:{_xirorigConfiguration.SeedNodePort}", _currentWorkingBlockTemplate);
                        }
                    }

                    await SendPacketToNetworkAsync(ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveAskCurrentBlockMining, true).ConfigureAwait(false);
                }
                else if (packet.StartsWith(ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.SendJobStatus))
                {
                    _lastValidPacketDateTimeOffset = DateTimeOffset.Now;

                    var packetData = packet.Split('|', StringSplitOptions.RemoveEmptyEntries);

                    if (packetData[1].Equals(ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.ShareUnlock))
                    {
                        BlockResult?.Invoke(true, "Share Accepted", packetData[2]);
                    }
                    else if (packetData[1].Equals(ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.ShareWrong))
                    {
                        BlockResult?.Invoke(false, "Invalid Share", packetData[2]);
                    }
                    else if (packetData[1].Equals(ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.ShareAleady))
                    {
                        BlockResult?.Invoke(false, "Orphan Share", packetData[2]);
                    }
                    else if (packetData[1].Equals(ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.ShareNotExist))
                    {
                        BlockResult?.Invoke(false, "Invalid Share", packetData[2]);
                    }
                    else if (packetData[1].Equals(ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.ShareGood))
                    {
                        BlockResult?.Invoke(true, "Share Accepted", packetData[2]);
                    }
                    else if (packetData[1].Equals(ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.ShareBad))
                    {
                        BlockResult?.Invoke(false, "Invalid/Orphan Share", packetData[2]);
                    }
                }
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
            _globalCancellationTokenSource?.Dispose();
            _tcpClient?.Dispose();
            _tcpClientReader?.Dispose();
            _tcpClientWriter?.Dispose();
            _semaphoreSlim.Dispose();
            _aes.Dispose();
            _aesEncryptor.Dispose();
            _aesDecryptor.Dispose();
            _checkConnectionNetworkTask?.Dispose();
            _readPacketFromNetworkTask?.Dispose();
        }
    }
}