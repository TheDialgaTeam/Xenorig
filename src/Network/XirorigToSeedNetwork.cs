using System;
using System.Buffers;
using System.Collections.Concurrent;
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

        private readonly Aes _aes;
        private readonly ICryptoTransform _aesEncryptor;
        private readonly ICryptoTransform _aesDecryptor;

        private JObject? _currentBlockTemplate;
        private JObject? _currentWorkingBlockTemplate;

        private Task? _checkConnectionNetworkTask;
        private Task? _readPacketFromNetworkTask;
        private Task? _writePacketToNetworkTask;

        private readonly ConcurrentQueue<PacketToSend> _loginConcurrentQueue = new ConcurrentQueue<PacketToSend>();
        private readonly ConcurrentQueue<PacketToSend> _submitBlockConcurrentQueue = new ConcurrentQueue<PacketToSend>();
        private readonly ConcurrentQueue<PacketToSend> _receiveBlockConcurrentQueue = new ConcurrentQueue<PacketToSend>();

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
            lock (_isActiveLock)
            {
                if (_isActive) return;
                _isActive = true;
            }

            _loginConcurrentQueue.Clear();
            _submitBlockConcurrentQueue.Clear();
            _receiveBlockConcurrentQueue.Clear();

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

            var linkedCancellationToken = _linkedCancellationTokenSource.Token;

            _checkConnectionNetworkTask = Task.Factory.StartNew(async state =>
            {
                if (!(state is (XirorigToSeedNetwork xirorigToSeedNetwork, CancellationToken cancellationTokenState))) return;

                while (xirorigToSeedNetwork._isActive)
                {
                    if (xirorigToSeedNetwork._connectionStatus == ConnectionStatus.Disconnected)
                    {
                        await xirorigToSeedNetwork.ConnectToSeedNetworkAsync().ConfigureAwait(false);
                    }

                    try
                    {
                        await Task.Delay(1, cancellationTokenState).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                    }
                }
            }, (this, linkedCancellationToken), linkedCancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap();
        }

        public async Task StopAsync()
        {
            lock (_isActiveLock)
            {
                if (!_isActive) return;
                _isActive = false;
            }

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

            if (_writePacketToNetworkTask != null)
            {
                await _writePacketToNetworkTask.ConfigureAwait(false);
                _writePacketToNetworkTask.Dispose();
                _writePacketToNetworkTask = null;
            }
        }

        public void EnqueuePacketToSent(in PacketToSend packet, PacketType packetType)
        {
            lock (_connectionStatusLock)
            {
                var connectionStatus = _connectionStatus;
                if (connectionStatus == ConnectionStatus.Disconnecting || connectionStatus == ConnectionStatus.Disconnected) return;
            }

            switch (packetType)
            {
                case PacketType.Login:
                    _loginConcurrentQueue.Enqueue(packet);
                    break;

                case PacketType.SubmitBlock:
                    _submitBlockConcurrentQueue.Enqueue(packet);
                    break;

                case PacketType.ReceiveBlockTemplate:
                    _receiveBlockConcurrentQueue.Enqueue(packet);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async Task ConnectToSeedNetworkAsync()
        {
            lock (_connectionStatusLock)
            {
                var connectionStatus = _connectionStatus;
                if (connectionStatus == ConnectionStatus.Connecting || connectionStatus == ConnectionStatus.Connected) return;
                _connectionStatus = ConnectionStatus.Connecting;
            }

            if (++_seedNodeIpAddressRetryCount > 5 && ++_seedNodeIpAddressIndex > _seedNodeIpAddresses.Length)
            {
                _seedNodeIpAddressIndex = 0;
            }

            var seedNodeIpAddress = _seedNodeIpAddresses[_seedNodeIpAddressIndex];
            var linkedCancellationToken = _linkedCancellationTokenSource!.Token;

            try
            {
                var tcpClient = new TcpClient();
                _tcpClient = tcpClient;

                using (var connectTimeoutCancellationTokenSource = new CancellationTokenSource(5000))
                {
                    using (var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(linkedCancellationToken, connectTimeoutCancellationTokenSource.Token))
                    {
                        await tcpClient.ConnectAsync(IPAddress.Parse(seedNodeIpAddress), ClassConnectorSetting.SeedNodePort, linkedCancellationTokenSource.Token).ConfigureAwait(false);
                    }
                }

                _tcpClientReader = new StreamReader(tcpClient.GetStream());
                _tcpClientWriter = new StreamWriter(tcpClient.GetStream());

                var walletAddress = _walletAddress;

                if (!_isWalletAddressValid)
                {
                    if (string.IsNullOrWhiteSpace(walletAddress))
                    {
                        LoginResult?.Invoke($"{seedNodeIpAddress}:{ClassConnectorSetting.SeedNodePort}", false);
                        await DisconnectFromSeedNetworkAsync().ConfigureAwait(false);
                        return;
                    }

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

                lock (_connectionStatusLock)
                {
                    _connectionStatus = ConnectionStatus.Connected;
                }

                StartReadingPacketFromNetwork();
                StartSendingPacketToNetwork();

                EnqueuePacketToSent(new PacketToSend(_connectionCertificate, false), PacketType.Login);
                EnqueuePacketToSent(new PacketToSend($"{ClassConnectorSettingEnumeration.MinerLoginType}|{walletAddress}", true), PacketType.Login);
            }
            catch (Exception)
            {
                await DisconnectFromSeedNetworkAsync().ConfigureAwait(false);
            }
        }

        private async Task DisconnectFromSeedNetworkAsync()
        {
            lock (_connectionStatusLock)
            {
                var connectionStatus = _connectionStatus;
                if (connectionStatus == ConnectionStatus.Disconnecting || connectionStatus == ConnectionStatus.Disconnected) return;
                _connectionStatus = ConnectionStatus.Disconnecting;
            }

            _tcpClientReader?.Dispose();

            if (_tcpClientWriter != null)
            {
                await _tcpClientWriter.DisposeAsync().ConfigureAwait(false);
            }

            _tcpClient?.Dispose();

            lock (_connectionStatusLock)
            {
                _connectionStatus = ConnectionStatus.Disconnected;
            }

            Disconnected?.Invoke($"{_seedNodeIpAddresses![_seedNodeIpAddressIndex]}:{ClassConnectorSetting.SeedNodePort}");
        }

        private void StartReadingPacketFromNetwork()
        {
            lock (_lastValidPacketDateTimeOffsetLock)
            {
                _lastValidPacketDateTimeOffset = DateTimeOffset.Now;
            }

            var cancellationToken = _linkedCancellationTokenSource!.Token;

            if (_readPacketFromNetworkTask != null)
            {
                _readPacketFromNetworkTask.Wait(cancellationToken);
                _readPacketFromNetworkTask.Dispose();
            }

            _readPacketFromNetworkTask = Task.Factory.StartNew(async state =>
            {
                if (!(state is (XirorigToSeedNetwork xirorigToSeedNetwork, CancellationToken cancellationTokenState))) return;

                var buffer = new char[ClassConnectorSetting.MaxNetworkPacketSize];
                var tcpClientReader = xirorigToSeedNetwork._tcpClientReader;

                while (xirorigToSeedNetwork._connectionStatus == ConnectionStatus.Connected)
                {
                    try
                    {
                        if (!(tcpClientReader!.BaseStream is NetworkStream networkStream)) continue;

                        if (DateTimeOffset.Now - xirorigToSeedNetwork._lastValidPacketDateTimeOffset >= TimeSpan.FromSeconds(5))
                        {
                            await xirorigToSeedNetwork.DisconnectFromSeedNetworkAsync().ConfigureAwait(false);
                            continue;
                        }

                        if (networkStream.DataAvailable)
                        {
                            var bufferSize = await tcpClientReader!.ReadAsync(buffer, 0, ClassConnectorSetting.MaxNetworkPacketSize).ConfigureAwait(false);
                            var decryptedPackets = GetDecryptedPackets(buffer, bufferSize);

                            _ = Task.Factory.StartNew(innerState =>
                            {
                                if (innerState is string[] decryptedPacketsState)
                                {
                                    HandlePacketFromNetwork(decryptedPacketsState);
                                }
                            }, decryptedPackets, cancellationTokenState, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                        }

                        try
                        {
                            await Task.Delay(1, cancellationTokenState).ConfigureAwait(false);
                        }
                        catch (TaskCanceledException)
                        {
                            break;
                        }
                    }
                    catch (Exception)
                    {
                        await xirorigToSeedNetwork.DisconnectFromSeedNetworkAsync().ConfigureAwait(false);
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

                if (!isEnd || index >= bufferSize) continue;

                var finalPacketBytes = Convert.FromBase64CharArray(buffer, index, bufferSize - index);
                var finalDecryptedPaddedBytes = aesDecryptor.TransformFinalBlock(finalPacketBytes, 0, finalPacketBytes.Length);

                result.Add(Encoding.UTF8.GetString(finalDecryptedPaddedBytes, 0, finalDecryptedPaddedBytes.Length - finalDecryptedPaddedBytes[^1]));
                break;
            } while (index < bufferSize);

            return result.ToArray();
        }

        private void HandlePacketFromNetwork(string[] packets)
        {
            foreach (var packet in packets)
            {
                if (packet.StartsWith(ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.SendLoginAccepted))
                {
                    lock (_lastValidPacketDateTimeOffsetLock)
                    {
                        _lastValidPacketDateTimeOffset = DateTimeOffset.Now;
                    }
                    
                    _seedNodeIpAddressRetryCount = 0;

                    LoginResult?.Invoke($"{_seedNodeIpAddresses![_seedNodeIpAddressIndex]}:{ClassConnectorSetting.SeedNodePort}", true);

                    EnqueuePacketToSent(new PacketToSend(ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveAskCurrentBlockMining, true), PacketType.ReceiveBlockTemplate);
                }
                else if (packet.StartsWith(ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.SendCurrentBlockMining))
                {
                    lock (_lastValidPacketDateTimeOffsetLock)
                    {
                        _lastValidPacketDateTimeOffset = DateTimeOffset.Now;
                    }

                    var currentBlockData = packet.Substring(packet.IndexOf('|') + 1).Split('&', StringSplitOptions.RemoveEmptyEntries);

                    var currentBlockTemplate = new JObject();
                    _currentBlockTemplate = currentBlockTemplate;

                    foreach (var data in currentBlockData)
                    {
                        var splitData = data.Split('=', StringSplitOptions.RemoveEmptyEntries);
                        currentBlockTemplate.Add(splitData[0], splitData[1]);
                    }

                    if (currentBlockTemplate.ContainsKey("METHOD"))
                    {
                        EnqueuePacketToSent(new PacketToSend($"{ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveAskContentBlockMethod}|{currentBlockTemplate["METHOD"]}", true), PacketType.ReceiveBlockTemplate);
                    }
                }
                else if (packet.StartsWith(ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.SendContentBlockMethod))
                {
                    lock (_lastValidPacketDateTimeOffsetLock)
                    {
                        _lastValidPacketDateTimeOffset = DateTimeOffset.Now;
                    }

                    var currentBlockData = packet.Substring(packet.IndexOf('|') + 1).Split('#', StringSplitOptions.RemoveEmptyEntries);
                    var currentBlockTemplate = _currentBlockTemplate;

                    currentBlockTemplate!.Add("AESROUND", currentBlockData[0]);
                    currentBlockTemplate.Add("AESSIZE", currentBlockData[1]);
                    currentBlockTemplate.Add("AESKEY", currentBlockData[2]);
                    currentBlockTemplate.Add("XORKEY", currentBlockData[3]);

                    if (!string.IsNullOrWhiteSpace(currentBlockTemplate["INDICATION"]!.Value<string>()))
                    {
                        if (_currentWorkingBlockTemplate == null || _currentWorkingBlockTemplate["INDICATION"]!.Value<string>() != currentBlockTemplate["INDICATION"]!.Value<string>())
                        {
                            _currentWorkingBlockTemplate = currentBlockTemplate;
                            NewJob?.Invoke($"{_seedNodeIpAddresses![_seedNodeIpAddressIndex]}:{ClassConnectorSetting.SeedNodePort}", currentBlockTemplate);
                        }
                    }

                    EnqueuePacketToSent(new PacketToSend(ClassSoloMiningPacketEnumeration.SoloMiningSendPacketEnumeration.ReceiveAskCurrentBlockMining, true), PacketType.ReceiveBlockTemplate);
                }
                else if (packet.StartsWith(ClassSoloMiningPacketEnumeration.SoloMiningRecvPacketEnumeration.SendJobStatus))
                {
                    lock (_lastValidPacketDateTimeOffsetLock)
                    {
                        _lastValidPacketDateTimeOffset = DateTimeOffset.Now;
                    }

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

        private void StartSendingPacketToNetwork()
        {
            var cancellationToken = _linkedCancellationTokenSource!.Token;

            if (_writePacketToNetworkTask != null)
            {
                _writePacketToNetworkTask.Wait(cancellationToken);
                _writePacketToNetworkTask.Dispose();
            }

            _writePacketToNetworkTask = Task.Factory.StartNew(async state =>
            {
                if (!(state is (XirorigToSeedNetwork xirorigToSeedNetwork, CancellationToken cancellationTokenState))) return;

                var loginConcurrentQueue = xirorigToSeedNetwork._loginConcurrentQueue;
                var submitBlockConcurrentQueue = xirorigToSeedNetwork._submitBlockConcurrentQueue;
                var receiveBlockConcurrentQueue = xirorigToSeedNetwork._receiveBlockConcurrentQueue;

                while (xirorigToSeedNetwork._connectionStatus == ConnectionStatus.Connected)
                {
                    if (loginConcurrentQueue.TryDequeue(out var packetToSend))
                    {
                        await SendPacketToNetworkAsync(packetToSend).ConfigureAwait(false);
                        continue;
                    }

                    if (submitBlockConcurrentQueue.TryDequeue(out packetToSend))
                    {
                        await SendPacketToNetworkAsync(packetToSend).ConfigureAwait(false);
                        continue;
                    }

                    if (receiveBlockConcurrentQueue.TryDequeue(out packetToSend))
                    {
                        await SendPacketToNetworkAsync(packetToSend).ConfigureAwait(false);
                        continue;
                    }

                    try
                    {
                        await Task.Delay(1, cancellationTokenState).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }, (this, cancellationToken), cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap();
        }

        private async Task SendPacketToNetworkAsync(PacketToSend packetToSend)
        {
            var tcpClientWriter = _tcpClientWriter;

            try
            {
                if (packetToSend.IsEncrypted)
                {
                    var packetBytes = Encoding.UTF8.GetBytes(packetToSend.Packet);
                    var packetLength = packetBytes.Length;

                    var paddingSizeRequired = 16 - packetLength % 16;
                    var arrayPoolByte = ArrayPool<byte>.Shared;
                    var paddedBytes = arrayPoolByte.Rent(packetLength + paddingSizeRequired);

                    try
                    {
                        Buffer.BlockCopy(packetBytes, 0, paddedBytes, 0, packetLength);

                        for (var i = 0; i < paddingSizeRequired; i++)
                        {
                            paddedBytes[packetLength + i] = (byte)paddingSizeRequired;
                        }

                        var encryptedPacketBytes = _aesEncryptor.TransformFinalBlock(paddedBytes, 0, packetLength + paddingSizeRequired);

                        await tcpClientWriter!.WriteAsync($"{Convert.ToBase64String(encryptedPacketBytes)}*").ConfigureAwait(false);
                        await tcpClientWriter.FlushAsync().ConfigureAwait(false);
                    }
                    finally
                    {
                        arrayPoolByte.Return(paddedBytes);
                    }
                }
                else
                {
                    await tcpClientWriter!.WriteAsync(packetToSend.Packet).ConfigureAwait(false);
                    await tcpClientWriter.FlushAsync().ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                await DisconnectFromSeedNetworkAsync().ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
            _linkedCancellationTokenSource?.Dispose();
            _tcpClient?.Dispose();
            _tcpClientReader?.Dispose();
            _tcpClientWriter?.Dispose();
            _aes.Dispose();
            _aesEncryptor.Dispose();
            _aesDecryptor.Dispose();
            _checkConnectionNetworkTask?.Dispose();
            _readPacketFromNetworkTask?.Dispose();
            _writePacketToNetworkTask?.Dispose();
        }
    }
}