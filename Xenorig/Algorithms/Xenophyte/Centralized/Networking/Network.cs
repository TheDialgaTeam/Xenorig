using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using Xenorig.Options;
using Xenorig.Utilities.KeyDerivationFunction;

namespace Xenorig.Algorithms.Xenophyte.Centralized;

internal partial class XenophyteCentralizedAlgorithm
{
    private static partial class Native
    {
        [DllImport(Program.XenoNativeLibrary)]
        public static extern void XenophyteCentralizedAlgorithm_GenerateCertificate([MarshalAs(UnmanagedType.LPStr)] string header, int keySize, in byte output);
    }

    private readonly Pool[] _pools;
    private int _poolIndex;
    private int _poolRetryCount;

    private int _isNetworkActive;
    private int _isNetworkConnected;
    private int _isNetworkConnecting;
    private int _isNetworkDisconnecting;

    private TcpClient? _tcpClient;
    private readonly byte[] _networkAesKey = new byte[MAJOR_UPDATE_1_SECURITY_CERTIFICATE_SIZE_ITEM / 8];
    private readonly byte[] _networkAesIv = new byte[16];

    private BlockingCollection<PacketData>? _packetDataCollection;
    private Thread? _packetHandlerThread;

    private static void GenerateCertificate(Span<byte> output)
    {
        Native.XenophyteCentralizedAlgorithm_GenerateCertificate(NETWORK_GENESIS_SECONDARY_KEY, MAJOR_UPDATE_1_SECURITY_CERTIFICATE_SIZE_ITEM, MemoryMarshal.GetReference(output));
    }

    private Task StartNetworkAsync(CancellationToken cancellationToken)
    {
        return Interlocked.CompareExchange(ref _isNetworkActive, 1, 0) == 1 ? Task.CompletedTask : Task.Run(async () => await ConnectToSeedNetworkAsync(cancellationToken), cancellationToken);
    }

    private Task StopNetworkAsync(CancellationToken cancellationToken)
    {
        return Interlocked.CompareExchange(ref _isNetworkActive, 0, 1) == 0 ? Task.CompletedTask : Task.Run(async () => await DisconnectFromSeedNetworkAsync(false, cancellationToken), cancellationToken);
    }

    private async Task ConnectToSeedNetworkAsync(CancellationToken cancellationToken = default)
    {
        if (_isNetworkActive == 0) return;
        if (_isNetworkConnected == 1) return;

        while (_isNetworkDisconnecting == 1)
        {
            await Task.Delay(1, cancellationToken);
        }

        if (Interlocked.CompareExchange(ref _isNetworkConnecting, 1, 0) == 1) return;

        do
        {
            try
            {
                if (++_poolRetryCount > _options.GetMaxRetryCount() && ++_poolIndex >= _pools.Length)
                {
                    _poolIndex = 0;
                }

                var selectedPool = _pools[_poolIndex];

                _tcpClient = new TcpClient();
                _tcpClient.ReceiveTimeout = (int) TimeSpan.FromSeconds(_options.GetTimeoutDuration()).TotalMilliseconds;
                _tcpClient.SendTimeout = (int) TimeSpan.FromSeconds(_options.GetTimeoutDuration()).TotalMilliseconds;
                await _tcpClient.ConnectAsync(selectedPool.GetUrl(), SeedNodePort, cancellationToken);

                using (var httpClient = new HttpClient { DefaultRequestHeaders = { UserAgent = { ProductInfoHeaderValue.Parse(selectedPool.GetUserAgent()) } } })
                {
                    var json = JsonNode.Parse(await httpClient.GetStreamAsync($"http://{selectedPool.GetUrl()}:{SeedNodeTokenPort}/{WalletTokenType}|{TokenCheckWalletAddressExist}|{selectedPool.GetUsername()}", cancellationToken));

                    if (json?["result"]?.GetValue<string>().Equals(SendTokenCheckWalletAddressInvalid, StringComparison.Ordinal) ?? true)
                    {
                        _poolRetryCount = _options.GetMaxRetryCount();
                        Logger.PrintLoginFailed(_logger, $"{selectedPool.GetUrl()}:{SeedNodePort}", "Invalid wallet address.");
                        continue;
                    }
                }

                // Connected
                Interlocked.Exchange(ref _isNetworkConnected, 1);

                DoConnectionCertificate(selectedPool);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                Logger.PrintDisconnected(_logger);
                await Task.Delay(1000, cancellationToken);
            }
        } while (_isNetworkConnected == 0 && _isNetworkActive == 1);

        Interlocked.Exchange(ref _isNetworkConnecting, 0);
    }

    private async Task DisconnectFromSeedNetworkAsync(bool retry = false, CancellationToken cancellationToken = default)
    {
        if (_isNetworkActive == 0) return;
        if (_isNetworkConnected == 0) return;

        while (_isNetworkConnecting == 1)
        {
            await Task.Delay(1, cancellationToken);
        }

        if (Interlocked.CompareExchange(ref _isNetworkDisconnecting, 1, 0) == 1) return;

        if (_packetDataCollection != null)
        {
            _packetDataCollection.CompleteAdding();

            while (!_packetDataCollection.IsCompleted)
            {
                await Task.Delay(1, cancellationToken);
            }
        }

        _tcpClient?.Dispose();

        Logger.PrintDisconnected(_logger);

        // Disconnected
        Interlocked.Exchange(ref _isNetworkConnected, 0);
        Interlocked.Exchange(ref _isNetworkDisconnecting, 0);

        if (retry)
        {
            await ConnectToSeedNetworkAsync(cancellationToken);
        }
    }

    private void SendPacketToNetwork(in PacketData packetData)
    {
        _packetDataCollection?.Add(packetData);
    }

    [SkipLocalsInit]
    private void DoConnectionCertificate(Pool selectedPool)
    {
        Span<byte> certificate = stackalloc byte[NETWORK_GENESIS_SECONDARY_KEY.Length + MAJOR_UPDATE_1_SECURITY_CERTIFICATE_SIZE_ITEM];
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
        _packetHandlerThread = new Thread(HandlePacketData) { IsBackground = true };
        _packetHandlerThread.Start();

        SendPacketToNetwork(new PacketData(certificate, false));

        SendPacketToNetwork(new PacketData($"{MinerLoginType}|{selectedPool.GetUsername()}", true, (packet, time) =>
        {
            if (Encoding.ASCII.GetString(packet).Equals(SendLoginAccepted))
            {
                Logger.PrintConnected(_logger, "SOLO", $"{selectedPool.GetUrl()}:{SeedNodePort}", time);
                GetNewBlockHeader();
            }
            else
            {
                DisconnectFromSeedNetworkAsync(true).GetAwaiter().GetResult();
            }
        }));
    }

    private void HandlePacketData()
    {
        if (_packetDataCollection == null || _tcpClient == null) return;

        while (!_packetDataCollection.IsCompleted)
        {
            var packetHandler = _packetDataCollection.Take();
            if (packetHandler.Execute(_tcpClient.GetStream(), _networkAesKey, _networkAesIv, out var _)) continue;

            DisconnectFromSeedNetworkAsync(true).GetAwaiter().GetResult();
            break;
        }
    }

    private void GetNewBlockHeader()
    {
        SendPacketToNetwork(new PacketData(ReceiveAskCurrentBlockMining, true, (packet, _) =>
        {
            if (!UpdateBlockHeader(packet))
            {
                DisconnectFromSeedNetworkAsync(true).GetAwaiter().GetResult();
                return;
            }

            SendPacketToNetwork(new PacketData($"{ReceiveAskContentBlockMethod}|{_blockMethod}", true, (innerPacket, _) =>
            {
                if (!UpdateBlockMethod(innerPacket))
                {
                    DisconnectFromSeedNetworkAsync(true).GetAwaiter().GetResult();
                    return;
                }

                GetNewBlockHeader();
            }));
        }));
    }
}