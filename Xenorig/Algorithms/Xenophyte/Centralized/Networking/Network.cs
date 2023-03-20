using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xenorig.Options;
using Xenorig.Utilities;
using Xenorig.Utilities.KeyDerivationFunction;

namespace Xenorig.Algorithms.Xenophyte.Centralized.Networking;

public delegate void NewBlockEventHandler(in BlockHeader blockHeader);

public sealed partial class Network : IDisposable
{
    public event NewBlockEventHandler? HasNewBlock;
    
    public bool IsNetworkActive => _isNetworkActive == 1;

    public bool IsNetworkConnected => _isNetworkConnected == 1;

    private static readonly byte[] CertificateSupportedCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789&~#@\'(\\)="u8.ToArray();

    private readonly ILogger _logger;
    private readonly XenorigOptions _options;
    private readonly SemaphoreSlim _connectionSemaphoreSlim = new(1, 1);

    private int _isNetworkActive;
    private int _isNetworkConnected;

    private CancellationTokenSource? _networkActiveCts;
    
    private readonly Pool _pool;
    private int _poolRetryCount;

    private TcpClient? _tcpClient;

    private readonly byte[] _networkAesKey = new byte[NetworkConstants.MajorUpdate1SecurityCertificateSizeItem / 8];
    private readonly byte[] _networkAesIv = new byte[16];

    private BlockingCollection<PacketData>? _packetDataCollection;
    private Thread? _packetHandlerThread;

    private int _blockHeight;
    private long _blockTimestampCreate;

    private string _blockMethod = string.Empty;
    private string _blockIndication = string.Empty;

    private long _blockDifficulty;
    private long _blockMinRange;
    private long _blockMaxRange;

    private byte[] _blockAesPassword = Array.Empty<byte>();
    private int _blockAesPasswordLength;

    private byte[] _blockAesSalt = Array.Empty<byte>();
    private int _blockAesSaltLength;

    private byte[] _blockAesKey = Array.Empty<byte>();
    private int _blockAesKeySize;

    private readonly byte[] _blockAesIv = new byte[16];

    private int _blockAesRound;

    private byte[] _blockXorKey = Array.Empty<byte>();
    private int _blockXorKeyLength;

    private string _currentBlockIndication = string.Empty;

    public Network(ILogger logger, XenorigOptions options, Pool pool)
    {
        _logger = logger;
        _options = options;
        _pool = pool;
    }

    private static void GenerateCertificate(Span<byte> output)
    {
        var index = Encoding.ASCII.GetBytes(NetworkConstants.NetworkGenesisSecondaryKey, output);
        var upperbound = CertificateSupportedCharacters.Length - 1;

        for (var i = NetworkConstants.MajorUpdate1SecurityCertificateSizeItem - 1; i >= 0; i--)
        {
            output.GetRef(index + i) = CertificateSupportedCharacters.GetRef(RandomNumberGeneratorUtility.GetRandomBetween(0, upperbound));
        }
    }
    
    [GeneratedRegex("(?<hostname>.+):?(?<port>\\d+)?$")]
    private static partial Regex GetHostnameAndPortRegex();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _isNetworkActive, 1, 0) == 1)
        {
            return Task.CompletedTask;
        }

        _networkActiveCts?.Dispose();
        _networkActiveCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        return ConnectAsync(_networkActiveCts.Token);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _isNetworkActive, 0, 1) == 0)
        {
            return Task.CompletedTask;
        }

        _networkActiveCts?.Cancel();
        return DisconnectAsync(cancellationToken);
    }

    public void SendPacketToNetwork(PacketData packetData)
    {
        try
        {
            if (!IsNetworkActive || !IsNetworkConnected) return;
            _packetDataCollection?.Add(packetData);
        }
        catch
        {
            // Drop this packet instead.
        }
    }

    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_networkActiveCts!.Token, cancellationToken);

        try
        {
            await _connectionSemaphoreSlim.WaitAsync(cancellationTokenSource.Token);

            if (!IsNetworkActive || IsNetworkConnected) return;

            do
            {
                _tcpClient = new TcpClient();
                _tcpClient.NoDelay = true;

                using (var networkTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.NetworkTimeoutDuration)))
                using (var connectionTimeoutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token, networkTimeoutCts.Token))
                {
                    try
                    {
                        if (IPEndPoint.TryParse(_pool.Url, out var ipEndPoint))
                        {
                            await _tcpClient.ConnectAsync(ipEndPoint.Address, ipEndPoint.Port == 0 ? NetworkConstants.SeedNodePort : ipEndPoint.Port, connectionTimeoutTokenSource.Token);
                                
                            using var httpClient = new HttpClient { DefaultRequestHeaders = { UserAgent = { ProductInfoHeaderValue.Parse(_pool.GetUserAgent()) } } };

                            var tokenNetwork = IPEndPoint.Parse(_pool.Url);
                            tokenNetwork.Port = NetworkConstants.SeedNodeTokenPort;
                                
                            var json = JsonNode.Parse(await httpClient.GetStreamAsync($"http://{tokenNetwork}/{NetworkConstants.WalletTokenType}|{NetworkConstants.TokenCheckWalletAddressExist}|{_pool.Username}", connectionTimeoutTokenSource.Token));

                            if (json?["result"]?.GetValue<string>().Equals(NetworkConstants.SendTokenCheckWalletAddressInvalid, StringComparison.Ordinal) ?? true)
                            {
                                _tcpClient.Dispose();
                                Logger.PrintLoginFailed(_logger, _pool.Url, "Invalid wallet address.");
                                continue;
                            }
                        }
                        else
                        {
                            var hostAndPortMatch = GetHostnameAndPortRegex().Match(_pool.Url);
                            var host = await Dns.GetHostAddressesAsync(hostAndPortMatch.Groups["hostname"].Value, connectionTimeoutTokenSource.Token);
                            await _tcpClient.ConnectAsync(host, hostAndPortMatch.Groups.ContainsKey("port") ? int.Parse(hostAndPortMatch.Groups["port"].Value) : NetworkConstants.SeedNodePort, connectionTimeoutTokenSource.Token);
                                
                            using var httpClient = new HttpClient { DefaultRequestHeaders = { UserAgent = { ProductInfoHeaderValue.Parse(_pool.GetUserAgent()) } } };

                            var json = JsonNode.Parse(await httpClient.GetStreamAsync($"{hostAndPortMatch.Groups["hostname"].Value}:{NetworkConstants.SeedNodeTokenPort}/{NetworkConstants.WalletTokenType}|{NetworkConstants.TokenCheckWalletAddressExist}|{_pool.Username}", connectionTimeoutTokenSource.Token));

                            if (json?["result"]?.GetValue<string>().Equals(NetworkConstants.SendTokenCheckWalletAddressInvalid, StringComparison.Ordinal) ?? true)
                            {
                                _tcpClient.Dispose();
                                Logger.PrintLoginFailed(_logger, _pool.Url, "Invalid wallet address.");
                                continue;
                            }
                        }
                    }
                    catch
                    {
                        _tcpClient.Dispose();
                        Logger.PrintLoginFailed(_logger, _pool.Url, "Connect Timeout.");
                        continue;
                    }
                }

                // Connected
                _poolRetryCount = 0;
                Interlocked.Exchange(ref _isNetworkConnected, 1);
                DoConnectionCertificate();
            } while (IsNetworkActive && !IsNetworkConnected && ++_poolRetryCount < _options.MaxRetryCount);
        }
        finally
        {
            _connectionSemaphoreSlim.Release();
        }
    }

    private async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_networkActiveCts!.Token, cancellationToken);

        try
        {
            await _connectionSemaphoreSlim.WaitAsync(cancellationTokenSource.Token);
            if (!IsNetworkActive || !IsNetworkConnected) return;

            _packetDataCollection?.CompleteAdding();
            _packetDataCollection?.Dispose();
            _tcpClient?.Dispose();

            Logger.PrintDisconnected(_logger);

            // Disconnected
            Interlocked.Exchange(ref _isNetworkConnected, 0);
        }
        finally
        {
            _connectionSemaphoreSlim.Release();
        }
    }

    private async Task ReconnectAsync(CancellationToken cancellationToken)
    {
        await DisconnectAsync(cancellationToken);
        await ConnectAsync(cancellationToken);
    }
    
    [SkipLocalsInit]
    private void DoConnectionCertificate()
    {
        Span<byte> certificate = stackalloc byte[NetworkConstants.NetworkGenesisSecondaryKey.Length + NetworkConstants.MajorUpdate1SecurityCertificateSizeItem];
        GenerateCertificate(certificate);

        var saltHex = Convert.ToHexString(certificate[..8]);
        Span<byte> salt = stackalloc byte[Encoding.ASCII.GetByteCount(saltHex)];
        Encoding.ASCII.GetBytes(saltHex, salt);

        using (var pbkdf1 = new PBKDF1(certificate, salt))
        {
            pbkdf1.FillBytes(_networkAesKey);
            pbkdf1.FillBytes(_networkAesIv);
        }

        _packetDataCollection = new BlockingCollection<PacketData>();
        _packetHandlerThread = new Thread(HandlePacketData) { IsBackground = true, Name = "Packet Handler" };
        _packetHandlerThread.Start();

        SendPacketToNetwork(new PacketData(certificate, false));

        SendPacketToNetwork(new PacketData($"{NetworkConstants.MinerLoginType}|{_pool.Username}", true, (packet, time) =>
        {
            if (Encoding.ASCII.GetString(packet).Equals(NetworkConstants.SendLoginAccepted))
            {
                Logger.PrintConnected(_logger, "SOLO", _pool.Url, time.TotalMilliseconds);
                GetNewBlockHeader();
            }
            else
            {
                // Login failed, reconnect.
                ReconnectAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
        }));
    }

    private async void HandlePacketData()
    {
        try
        {
            do
            {
                var packetHandler = _packetDataCollection!.Take(_networkActiveCts!.Token);

                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(_options.NetworkTimeoutDuration));
                var executeTask = Task.Run(() => packetHandler.Execute(_tcpClient!.GetStream(), _networkAesKey, _networkAesIv));

                var completedTask = await Task.WhenAny(timeoutTask, executeTask);

                if (completedTask == timeoutTask)
                {
                    // Send and Receive Timeout, reconnect.
                    await ReconnectAsync(CancellationToken.None);
                    break;
                }

                if (await executeTask) continue;

                // Execute failed, reconnect.
                await ReconnectAsync(CancellationToken.None);
                break;
            } while (!_packetDataCollection.IsCompleted);
        }
        catch
        {
            // Packet data is disposed, stop executing.
        }
    }

    private void GetNewBlockHeader()
    {
        SendPacketToNetwork(new PacketData(NetworkConstants.ReceiveAskCurrentBlockMining, true, ReceiveBlockHeaderPacketHandler));
    }

    private void ReceiveBlockHeaderPacketHandler(ReadOnlySpan<byte> packet, TimeSpan roundTripTime)
    {
        if (!UpdateBlockHeader(packet))
        {
            ReconnectAsync(CancellationToken.None).GetAwaiter().GetResult();
            return;
        }

        SendPacketToNetwork(new PacketData($"{NetworkConstants.ReceiveAskContentBlockMethod}|{_blockMethod}", true, ReceiveBlockMethodPacketHandler));
    }

    private void ReceiveBlockMethodPacketHandler(ReadOnlySpan<byte> packet, TimeSpan roundTripTime)
    {
        if (!UpdateBlockMethod(packet))
        {
            ReconnectAsync(CancellationToken.None).GetAwaiter().GetResult();
            return;
        }

        GetNewBlockHeader();
    }

    [SkipLocalsInit]
    private bool UpdateBlockHeader(ReadOnlySpan<byte> packet)
    {
        // SEND-CURRENT-BLOCK-MINING|
        // ID=1050878&
        // HASH=8D67ED6841B2305E0B23A5D009015F466BCBCEA0B5541263E0682B2E576B0CB9&
        // ALGORITHM=AES&
        // SIZE=256&
        // METHOD=XENOPHYTE&
        // KEY=Uos4dc09b3pgQGnII985eLmfLRKW6gG5FC3J6ctarAnRe2Uhof0a5Rx5vBi0JekuKz1ufEzJmp1ZouJC90sMFcr62WgGgidV5XLX12llxw4WuerTp4ZZdrOm82m075walihO54MEP8na2VYfRkJU344wzbbG0XjqtsSp3Tlh2UbJbYJr7KxjVlMVf9hRo344gG8xLRb98jpP5dkAnAAKBlJJZcqHgz6JqOamhOegwdUSurYP48wIZxbCKPTWeAtY&
        // JOB=2;10107&
        // REWARD=10.00000000&
        // DIFFICULTY=10107&
        // TIMESTAMP=1654187614&
        // INDICATION=ECBC42F0E83175E174FA6AB566EBEA88D97C2195BDB6E19483CDAD1FFC8EAF2A657BB8EF53E0D04FAFEF7539F7E9724F820517F560829288C0E04B4C58DD693B&
        // NETWORK_HASHRATE=12128&
        // LIFETIME=360

        Span<char> packetString = stackalloc char[Encoding.ASCII.GetCharCount(packet)];
        Encoding.ASCII.GetChars(packet, packetString);

        if (!packetString[..NetworkConstants.SendCurrentBlockMining.Length].SequenceEqual(NetworkConstants.SendCurrentBlockMining)) return false;

        ReadOnlySpan<char> current = packetString[(NetworkConstants.SendCurrentBlockMining.Length + 1)..];

        while (current.Length > 0)
        {
            var keyIndex = current.IndexOf('=');
            if (keyIndex == -1) return false;

            var key = current[..keyIndex];

            current = current[(keyIndex + 1)..];

            var valueIndex = current.IndexOf('&');
            var value = current[..current.Length];

            if (valueIndex > 0)
            {
                value = current[..valueIndex];
                current = current[(valueIndex + 1)..];
            }
            else
            {
                current = ReadOnlySpan<char>.Empty;
            }

            switch (key)
            {
                case "ID":
                    _blockHeight = int.Parse(value);
                    break;

                case "METHOD" when value.SequenceEqual(_blockMethod):
                    continue;

                case "METHOD":
                    _blockMethod = value.ToString();
                    break;

                case "DIFFICULTY":
                    _blockDifficulty = long.Parse(value);
                    break;

                case "TIMESTAMP":
                    _blockTimestampCreate = long.Parse(value);
                    break;

                case "KEY":
                {
                    var aesPasswordLength = Encoding.ASCII.GetByteCount(value);

                    if (_blockAesPassword.Length < aesPasswordLength)
                    {
                        _blockAesPassword = GC.AllocateUninitializedArray<byte>(aesPasswordLength);
                    }

                    _blockAesPasswordLength = Encoding.ASCII.GetBytes(value, _blockAesPassword);
                    break;
                }

                case "INDICATION" when value.SequenceEqual(_blockIndication):
                    continue;

                case "INDICATION":
                    _blockIndication = value.ToString();
                    break;

                case "JOB":
                {
                    var index = value.IndexOf(';');
                    _blockMinRange = long.Parse(value[..index]);
                    _blockMaxRange = long.Parse(value[(index + 1)..]);
                    break;
                }
            }
        }

        return true;
    }

    [SkipLocalsInit]
    private bool UpdateBlockMethod(ReadOnlySpan<byte> packet)
    {
        // SEND-CONTENT-BLOCK-METHOD|
        // 1#128#128#128

        Span<char> packetString = stackalloc char[Encoding.ASCII.GetCharCount(packet)];
        Encoding.ASCII.GetChars(packet, packetString);

        if (!packetString[..NetworkConstants.SendContentBlockMethod.Length].SequenceEqual(NetworkConstants.SendContentBlockMethod)) return false;

        ReadOnlySpan<char> current = packetString[(NetworkConstants.SendContentBlockMethod.Length + 1)..];

        var index = current.IndexOf('#');
        if (index < 0) return false;

        _blockAesRound = int.Parse(current[..index]);

        current = current[(index + 1)..];

        index = current.IndexOf('#');
        if (index < 0) return false;

        _blockAesKeySize = int.Parse(current[..index]);

        if (_blockAesKey.Length < _blockAesKeySize)
        {
            _blockAesKey = GC.AllocateUninitializedArray<byte>(_blockAesKeySize);
        }

        current = current[(index + 1)..];

        index = current.IndexOf('#');
        if (index < 0) return false;

        var aesSaltLength = Encoding.ASCII.GetByteCount(current[..index]);

        if (_blockAesSalt.Length < aesSaltLength)
        {
            _blockAesSalt = GC.AllocateUninitializedArray<byte>(aesSaltLength);
        }

        _blockAesSaltLength = Encoding.ASCII.GetBytes(current[..index], _blockAesSalt);

        current = current[(index + 1)..];

        var xorKeyLength = Encoding.ASCII.GetByteCount(current);

        if (_blockXorKey.Length < xorKeyLength)
        {
            _blockXorKey = GC.AllocateUninitializedArray<byte>(xorKeyLength);
        }

        _blockXorKeyLength = Encoding.ASCII.GetBytes(current, _blockXorKey);

        using (var pbkdf1 = new PBKDF1(_blockAesPassword.AsSpan(0, _blockAesPasswordLength), _blockAesSalt.AsSpan(0, _blockAesSaltLength)))
        {
            pbkdf1.FillBytes(_blockAesKey.AsSpan(0, _blockAesKeySize / 8));
            pbkdf1.FillBytes(_blockAesIv);
        }

        if (_currentBlockIndication == _blockIndication) return true;

        _currentBlockIndication = _blockIndication;
        Logger.PrintJob(_logger, "new job", _pool.Url, _blockDifficulty, _blockMethod, _blockHeight);

        Span<byte> tempXorKey = stackalloc byte[_blockXorKeyLength];
        _blockXorKey.AsSpan(0, _blockXorKeyLength).CopyTo(tempXorKey);

        Span<byte> tempAesKey = stackalloc byte[_blockAesKeySize];
        _blockAesKey.AsSpan(0, _blockAesKeySize).CopyTo(tempAesKey);

        Span<byte> tempAesIv = stackalloc byte[16];
        _blockAesIv.AsSpan().CopyTo(tempAesIv);

        var blockHeader = new BlockHeader
        {
            Height = _blockHeight,
            TimestampCreate = _blockTimestampCreate,

            Method = _blockMethod,
            Indication = _blockIndication,

            Difficulty = _blockDifficulty,
            MinRange = _blockMinRange,
            MaxRange = _blockMaxRange,

            XorKey = tempXorKey,

            AesKey = tempAesKey,
            AesIv = tempAesIv,

            AesRound = _blockAesRound
        };
        
        HasNewBlock?.Invoke(blockHeader);
        
        return true;
    }

    public void Dispose()
    {
        _networkActiveCts?.Dispose();
        _connectionSemaphoreSlim.Dispose();
    }
}