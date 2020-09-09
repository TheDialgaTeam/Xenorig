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

        private readonly ILogger<XirorigToSeedNetwork> _logger;

        private string[] _seedNodeIpAddresses;
        private int _seedNodeIpAddressIndex;
        private int _seedNodeIpAddressRetryCount;

        private readonly object _isActiveLock = new object();
        private readonly object _connectionStatusLock = new object();
        private readonly object _lastValidPacketDateTimeOffsetLock = new object();

        private bool _isActive;
        private ConnectionStatus _connectionStatus = ConnectionStatus.Disconnected;
        private DateTimeOffset _lastValidPacketDateTimeOffset = DateTimeOffset.Now;

        private CancellationTokenSource? _cancellationTokenSource;
        private CancellationTokenSource? _linkedCancellationTokenSource;

        private TcpClient? _tcpClient;
        private StreamReader? _tcpClientReader;
        private StreamWriter? _tcpClientWriter;

        private readonly string _connectionCertificate;
        private readonly string? _walletAddress;
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
            _logger = logger;

            _seedNodeIpAddresses = xirorigConfiguration.SeedNodeIpAddresses;
            _walletAddress = xirorigConfiguration.WalletAddress;

            _connectionCertificate = ClassUtils.GenerateCertificate();

            using var password = new PasswordDeriveBytes(_connectionCertificate, Encoding.UTF8.GetBytes(ClassUtils.FromHex(_connectionCertificate.Substring(0, 8))));
#pragma warning disable 618
            var aesIvCertificate = password.GetBytes(ClassConnectorSetting.MAJOR_UPDATE_1_SECURITY_CERTIFICATE_SIZE / 8);
            var aesSaltCertificate = password.GetBytes(16);
#pragma warning restore 618

            var aes = Aes.Create();
            aes.Mode = CipherMode.CFB;
            aes.BlockSize = 128;
            aes.Padding = PaddingMode.None;
            aes.KeySize = ClassConnectorSetting.MAJOR_UPDATE_1_SECURITY_CERTIFICATE_SIZE;
            aes.Key = aesIvCertificate;

            _aesEncryptor = aes.CreateEncryptor(aesIvCertificate, aesSaltCertificate);
            _aesDecryptor = aes.CreateDecryptor(aesIvCertificate, aesSaltCertificate);
            _aes = aes;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_isActive) return;
            _isActive = true;

            var cancellationTokenSource = new CancellationTokenSource();
            _cancellationTokenSource = cancellationTokenSource;
            _linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellationTokenSource.Token);

            var seedNodeIpAddressesPing = new Dictionary<string, long>();

            using (var ping = new Ping())
            {
                var seedNodeIpAddresses = _seedNodeIpAddresses;

                foreach (var seedNodeIpAddress in seedNodeIpAddresses)
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
            var newSeedNodeIpAddresses = new List<string>();
            var index = 1;

            foreach (var (ipAddress, ping) in sortedSeedNodeIpAddressesPing)
            {
                if (ping == -1) continue;

                newSeedNodeIpAddresses.Add(ipAddress);
                _logger.LogInformation(" \u001b[32;1m*\u001b[0m SOLO #{index,-7:l}\u001b[36;1m{nodeIp:l}\u001b[0m ({nodePing}ms)", index.ToString(), ipAddress, ping);
                index++;
            }

            _seedNodeIpAddresses = newSeedNodeIpAddresses.ToArray();

            CheckNetworkConnection();
        }

        public async Task StopAsync()
        {
            if (!IsActive) return;
            IsActive = false;

            _cancellationTokenSource!.Cancel();
            _cancellationTokenSource!.Dispose();
            _linkedCancellationTokenSource!.Dispose();

            _cancellationTokenSource = null;
            _linkedCancellationTokenSource = null;

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
                if (_connectionStatus == ConnectionStatus.Disconnecting || _connectionStatus == ConnectionStatus.Disconnected) return;

                if (isEncrypted)
                {
                    var packetBytes = Encoding.UTF8.GetBytes(packet);
                    var packetLength = packetBytes.Length;

                    var paddingSizeRequired = 16 - packetLength % 16;
                    var paddedBytes = ArrayPool<byte>.Shared.Rent(packetLength + paddingSizeRequired);

                    try
                    {
                        Buffer.BlockCopy(packetBytes, 0, paddedBytes, 0, packetLength);

                        for (var i = 0; i < paddingSizeRequired; i++)
                        {
                            paddedBytes[packetLength + i] = (byte) paddingSizeRequired;
                        }

                        try
                        {
                            await _semaphoreSlim.WaitAsync(_linkedCancellationTokenSource!.Token).ConfigureAwait(false);
                        }
                        catch (Exception)
                        {
                            return;
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
                    try
                    {
                        await _semaphoreSlim.WaitAsync(_linkedCancellationTokenSource!.Token).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        return;
                    }

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

        private void CheckNetworkConnection()
        {
            if (_checkConnectionNetworkTask != null) return;

            var cancellationToken = _linkedCancellationTokenSource!.Token;

            _checkConnectionNetworkTask = Task.Factory.StartNew(async state =>
            {
                if (!(state is (XirorigToSeedNetwork xirorigToSeedNetwork, CancellationToken cancellationTokenState))) return;

                while (xirorigToSeedNetwork.IsActive && !cancellationTokenState.IsCancellationRequested)
                {
                    switch (xirorigToSeedNetwork.ConnectionStatus)
                    {
                        case ConnectionStatus.Connected when DateTimeOffset.Now - xirorigToSeedNetwork.LastValidPacketDateTimeOffset > TimeSpan.FromSeconds(5000):
                            await xirorigToSeedNetwork.DisconnectFromSeedNetworkAsync().ConfigureAwait(false);
                            break;

                        case ConnectionStatus.Disconnected:
                            await xirorigToSeedNetwork.ConnectToSeedNetworkAsync().ConfigureAwait(false);
                            break;

                        case ConnectionStatus.Connecting:
                            break;

                        case ConnectionStatus.Disconnecting:
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    try
                    {
                        await Task.Delay(1000, cancellationTokenState).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                        return;
                    }
                }
            }, (this, cancellationToken), cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap();
        }

        private async Task ConnectToSeedNetworkAsync()
        {
            var currentConnectionStatus = ConnectionStatus;
            if (currentConnectionStatus == ConnectionStatus.Connecting || currentConnectionStatus == ConnectionStatus.Connected) return;

            ConnectionStatus = ConnectionStatus.Connecting;

            if (++_seedNodeIpAddressRetryCount > 5 && ++_seedNodeIpAddressIndex > _seedNodeIpAddresses.Length)
            {
                _seedNodeIpAddressIndex = 0;
            }

            var seedNodeIpAddress = _seedNodeIpAddresses[_seedNodeIpAddressIndex];
            var linkedCancellationToken = _linkedCancellationTokenSource!.Token;

            try
            {
                _tcpClient = new TcpClient();

                using (var connectTimeoutCancellationTokenSource = new CancellationTokenSource(5000))
                {
                    using (var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(linkedCancellationToken, connectTimeoutCancellationTokenSource.Token))
                    {
                        await _tcpClient.ConnectAsync(IPAddress.Parse(seedNodeIpAddress), ClassConnectorSetting.SeedNodePort, linkedCancellationTokenSource.Token).ConfigureAwait(false);
                    }
                }

                _tcpClientReader = new StreamReader(_tcpClient.GetStream());
                _tcpClientWriter = new StreamWriter(_tcpClient.GetStream());

                var walletAddress = _walletAddress;

                if (!_isWalletAddressValid)
                {
                    var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5), BaseAddress = new Uri($"http://{seedNodeIpAddress}:{ClassConnectorSetting.SeedNodeTokenPort}/") };
                    httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Xirorig", Assembly.GetExecutingAssembly().GetName().Version?.ToString()));

                    var jsonString = await httpClient.GetStringAsync($"{ClassConnectorSettingEnumeration.WalletTokenType}|{ClassRpcWalletCommand.TokenCheckWalletAddressExist}|{walletAddress}", linkedCancellationToken).ConfigureAwait(false);
                    var json = JObject.Parse(jsonString);

                    if (json.TryGetValue("result", StringComparison.OrdinalIgnoreCase, out var resultToken))
                    {
                        if (resultToken.Value<string>().StartsWith(ClassRpcWalletCommand.SendTokenCheckWalletAddressInvalid))
                        {
                            LoginResult?.Invoke($"{seedNodeIpAddress}:{ClassConnectorSetting.SeedNodePort}", false);
                            await DisconnectFromSeedNetworkAsync().ConfigureAwait(false);
                            return;
                        }
                    }

                    _isWalletAddressValid = true;
                }

                ConnectionStatus = ConnectionStatus.Connected;
                StartReadingPacketFromNetwork();

                await SendPacketToNetworkAsync(_connectionCertificate, false).ConfigureAwait(false);
                await SendPacketToNetworkAsync($"{ClassConnectorSettingEnumeration.MinerLoginType}|{walletAddress}", true).ConfigureAwait(false);
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

        private void StartReadingPacketFromNetwork()
        {
            LastValidPacketDateTimeOffset = DateTimeOffset.Now;
            var cancellationToken = _linkedCancellationTokenSource!.Token;

            _readPacketFromNetworkTask = Task.Factory.StartNew(async state =>
            {
                if (state is (XirorigToSeedNetwork xirorigToSeedNetwork, CancellationToken cancellationTokenState))
                {
                    var buffer = new char[ClassConnectorSetting.MaxNetworkPacketSize];
                    var tcpClientReader = xirorigToSeedNetwork._tcpClientReader;

                    while (xirorigToSeedNetwork.IsActive && !cancellationTokenState.IsCancellationRequested && xirorigToSeedNetwork.ConnectionStatus == ConnectionStatus.Connected)
                    {
                        try
                        {
                            if (!(tcpClientReader!.BaseStream is NetworkStream networkStream)) continue;

                            if (networkStream.DataAvailable)
                            {
                                var bufferSize = await tcpClientReader!.ReadAsync(buffer, 0, ClassConnectorSetting.MaxNetworkPacketSize).ConfigureAwait(false);
                                var decryptedPackets = GetDecryptedPackets(buffer, bufferSize);

                                _ = Task.Factory.StartNew(async innerState =>
                                {
                                    if (innerState is string[] decryptedPacketsState)
                                    {
                                        await HandlePacketFromNetworkAsync(decryptedPacketsState).ConfigureAwait(false);
                                    }
                                }, decryptedPackets, cancellationTokenState, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                            }

                            await Task.Delay(1, cancellationTokenState).ConfigureAwait(false);
                        }
                        catch (Exception)
                        {
                            if (cancellationTokenState.IsCancellationRequested) return;
                            await xirorigToSeedNetwork.DisconnectFromSeedNetworkAsync().ConfigureAwait(false);
                        }
                    }
                }
            }, (this, cancellationToken), cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap();
        }

        private string[] GetDecryptedPackets(char[] buffer, int bufferSize)
        {
            var result = new List<string>();
            var aesDecryptor = _aesDecryptor;
            var index = 0;

            do
            {
                var isEnd = true;

                for (var i = index; i < bufferSize; i++)
                {
                    if (buffer[i] != '*') continue;

                    var packetBytes = Convert.FromBase64CharArray(buffer, index, i - index);
                    var decryptedPaddedBytes = aesDecryptor.TransformFinalBlock(packetBytes, 0, packetBytes.Length);
                    
                    result.Add(Encoding.UTF8.GetString(decryptedPaddedBytes, 0, decryptedPaddedBytes.Length - decryptedPaddedBytes[^1]));

                    index = i + 1;
                    isEnd = false;
                    break;
                }

                if (!isEnd) continue;
                {
                    var packetBytes = Convert.FromBase64CharArray(buffer, index, bufferSize - index);
                    var decryptedPaddedBytes = aesDecryptor.TransformFinalBlock(packetBytes, 0, packetBytes.Length);

                    result.Add(Encoding.UTF8.GetString(decryptedPaddedBytes, 0, decryptedPaddedBytes.Length - decryptedPaddedBytes[^1]));
                    break;
                }
            } while (index < bufferSize);

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
            _linkedCancellationTokenSource?.Dispose();
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