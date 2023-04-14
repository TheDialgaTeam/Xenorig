using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Options;
using Xenolib.Algorithms.Xenophyte.Centralized.Networking;
using Xenolib.Utilities;
using Xenolib.Utilities.Buffer;
using Xenolib.Utilities.KeyDerivationFunction;
using Xenopool.Server.Networking.RpcWallet;
using Xenopool.Server.Options;

namespace Xenopool.Server.Networking.SoloMining;

public sealed class SoloMiningNetwork : IDisposable
{
    public BlockHeader BlockHeader { get; } = new();

    private static readonly byte[] CertificateSupportedCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789&~#@\'(\\)="u8.ToArray();

    private readonly ILogger<SoloMiningNetwork> _logger;
    
    private readonly IDisposable? _disposable;
    private Options.SoloMining _soloMiningOptions;
    private Options.RpcWallet _rpcWalletOptions;

    private int _isNetworkActive;

    private TcpClient? _tcpClient;

    private readonly SemaphoreSlim _connectionSemaphoreSlim = new(1, 1);

    private readonly byte[] _networkAesKey = new byte[NetworkConstants.MajorUpdate1SecurityCertificateSizeItem / 8];
    private readonly byte[] _networkAesIv = new byte[16];

    private BlockingCollection<PacketData>? _packetDataBlockingCollection;
    private Task? _networkPacketHandlerTask;

    public SoloMiningNetwork(ILogger<SoloMiningNetwork> logger, IOptionsMonitor<XenopoolOptions> optionsMonitor, RpcWalletNetwork rpcWalletNetwork)
    {
        _logger = logger;
        _disposable = optionsMonitor.OnChange(OnOptionsChanged);
        
        OnOptionsChanged(optionsMonitor.CurrentValue);
    }

    private static ArrayPoolOwner<byte> GenerateCertificate()
    {
        var requestedLength = Encoding.UTF8.GetByteCount(NetworkConstants.NetworkGenesisSecondaryKey) + NetworkConstants.MajorUpdate1SecurityCertificateSizeItem;
        var outputArrayPoolOwner = ArrayPoolOwner<byte>.Rent(requestedLength);
        var outputSpan = outputArrayPoolOwner.Span;

        var index = Encoding.UTF8.GetBytes(NetworkConstants.NetworkGenesisSecondaryKey, outputSpan);
        var upperbound = CertificateSupportedCharacters.Length - 1;

        for (var i = NetworkConstants.MajorUpdate1SecurityCertificateSizeItem - 1; i >= 0; i--)
        {
            outputSpan.GetRef(index + i) = CertificateSupportedCharacters.GetRef(RandomNumberGeneratorUtility.GetRandomBetween(0, upperbound));
        }

        return outputArrayPoolOwner;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _isNetworkActive, 1, 0) == 1)
        {
            return;
        }

        await ConnectAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _isNetworkActive, 0, 1) == 0)
        {
            return;
        }
        
        await DisconnectAsync(cancellationToken);
    }

    public void SendPacketToNetwork(PacketData packetData)
    {
        try
        {
            _packetDataBlockingCollection?.Add(packetData);
        }
        catch
        {
            // Assumed it is disconnected hence you cant add anyway.
        }
    }

    private async void OnOptionsChanged(XenopoolOptions options)
    {
        _soloMiningOptions = options.SoloMining;
        _rpcWalletOptions = options.RpcWallet;

        if (_isNetworkActive != 1) return;
        
        await StopAsync();
        await StartAsync();
    }

    private async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _connectionSemaphoreSlim.WaitAsync(cancellationToken);
            await InternalConnectAsync(cancellationToken);
        }
        finally
        {
            _connectionSemaphoreSlim.Release();
        }
    }

    private async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _connectionSemaphoreSlim.WaitAsync(cancellationToken);
            InternalDisconnect();
        }
        finally
        {
            _connectionSemaphoreSlim.Release();
        }
    }

    private async Task InternalConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_tcpClient != null) return;

        _tcpClient = new TcpClient();

        using var networkTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_soloMiningOptions.NetworkTimeoutDuration));
        using var connectionTimeoutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, networkTimeoutCts.Token);

        try
        {
            await _tcpClient.ConnectAsync(_soloMiningOptions.Host, _soloMiningOptions.Port == 0 ? NetworkConstants.SeedNodePort : _soloMiningOptions.Port, connectionTimeoutTokenSource.Token);

            if (_tcpClient.Connected)
            {
                UpdateStatus(true, string.Empty);
                
                _packetDataBlockingCollection = new BlockingCollection<PacketData>();
                _networkPacketHandlerTask = Task.Factory.StartNew(NetworkPacketHandlerTask, TaskCreationOptions.LongRunning);

                var certificateArrayPoolOwner = GenerateCertificate();

                var saltHex = Convert.ToHexString(certificateArrayPoolOwner.Span[..8]);
                using var saltArrayPoolOwner = ArrayPoolOwner<byte>.Rent(Encoding.UTF8.GetByteCount(saltHex));
                Encoding.UTF8.GetBytes(saltHex, saltArrayPoolOwner.Span);

                using (var pbkdf1 = new PBKDF1(certificateArrayPoolOwner.Span, saltArrayPoolOwner.Span))
                {
                    pbkdf1.FillBytes(_networkAesKey);
                    pbkdf1.FillBytes(_networkAesIv);
                }

                SendPacketToNetwork(new PacketData(certificateArrayPoolOwner, false));

                SendPacketToNetwork(new PacketData($"{NetworkConstants.MinerLoginType}|{_rpcWalletOptions.WalletAddress}", true, (packet, _) =>
                {
                    if (Encoding.UTF8.GetString(packet).Equals(NetworkConstants.SendLoginAccepted))
                    {
                        // Ready?.Invoke();
                    }
                    else
                    {
                        InternalDisconnect("Login failed");
                    }
                }));
            }
            else
            {
                InternalDisconnect("Connection failed");
            }
        }
        catch (OperationCanceledException)
        {
            InternalDisconnect("Connection timeout");
        }
        catch
        {
            InternalDisconnect("Connection failed");
        }
    }

    private void InternalDisconnect(string reason = "")
    {
        _packetDataBlockingCollection?.CompleteAdding();

        _networkPacketHandlerTask?.Wait();
        _networkPacketHandlerTask?.Dispose();
        _networkPacketHandlerTask = null;

        _packetDataBlockingCollection?.Dispose();
        _packetDataBlockingCollection = null;

        _tcpClient?.Dispose();
        _tcpClient = null;

        UpdateStatus(false, reason);
    }

    private async Task NetworkPacketHandlerTask()
    {
        while (_packetDataBlockingCollection is { IsAddingCompleted: false })
        {
            try
            {
                using var packetData = _packetDataBlockingCollection.Take();

                try
                {
                    if (_tcpClient == null) return;

                    using var networkTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_soloMiningOptions.NetworkTimeoutDuration));

                    if (!await packetData.ExecuteAsync(_tcpClient.GetStream(), _networkAesKey, _networkAesIv, networkTimeoutCts.Token))
                    {
                        InternalDisconnect("Packet handler error");
                    }
                }
                catch (OperationCanceledException)
                {
                    InternalDisconnect("Packet handler timeout");
                }
                catch
                {
                    InternalDisconnect("Packet handler error");
                }
            }
            catch
            {
                // It is ended and so, there is no point in handling anything else, just assumed it is disconnected.
            }
        }
    }

    private async void UpdateStatus(bool isConnected, string reason)
    {
        if (_isNetworkActive != 1) return;
        if (isConnected) return;
        
        Logger.PrintDisconnected(_logger, _soloMiningOptions.Host, reason == string.Empty ? "None" : reason);
        await ConnectAsync(CancellationToken.None);
    }
    
    public void Dispose()
    {
        InternalDisconnect();
        _disposable?.Dispose();
        _connectionSemaphoreSlim.Dispose();
    }
}