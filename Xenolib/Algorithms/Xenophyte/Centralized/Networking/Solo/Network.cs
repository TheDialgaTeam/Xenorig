using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Text;
using Xenolib.Utilities;
using Xenolib.Utilities.Buffer;
using Xenolib.Utilities.KeyDerivationFunction;

namespace Xenolib.Algorithms.Xenophyte.Centralized.Networking.Solo;

public delegate void DisconnectHandler(string reason);

public delegate void NewBlockHandler(BlockHeader blockHeader);

[UnsupportedOSPlatform("browser")]
public sealed class Network : IDisposable
{
    public event DisconnectHandler? Disconnected;
    public event Action? Ready;
    public event NewBlockHandler? HasNewBlock;

    private static readonly byte[] CertificateSupportedCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789&~#@\'(\\)="u8.ToArray();

    private TcpClient? _tcpClient;
    private NetworkConnection? _networkConnection;

    private readonly SemaphoreSlim _connectionSemaphoreSlim = new(1, 1);

    private readonly byte[] _networkAesKey;
    private readonly byte[] _networkAesIv;

    private BlockingCollection<PacketData>? _packetDataBlockingCollection;
    private Task? _networkPacketHandlerTask;

    private readonly BlockHeader _blockHeader = new();

    public Network()
    {
        _networkAesKey = GC.AllocateUninitializedArray<byte>(NetworkConstants.MajorUpdate1SecurityCertificateSizeItem / 8);
        _networkAesIv = GC.AllocateUninitializedArray<byte>(16);
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
    
    public async Task ConnectAsync(NetworkConnection networkConnection, CancellationToken cancellationToken = default)
    {
        try
        {
            await _connectionSemaphoreSlim.WaitAsync(cancellationToken);
            await InternalConnectAsync(networkConnection, cancellationToken);
        }
        finally
        {
            _connectionSemaphoreSlim.Release();
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
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
    
    public bool SendPacketToNetwork(PacketData packetData)
    {
        try
        {
            _packetDataBlockingCollection?.Add(packetData);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task InternalConnectAsync(NetworkConnection networkConnection, CancellationToken cancellationToken = default)
    {
        if (_tcpClient != null) return;

        _tcpClient = new TcpClient();
        _networkConnection = networkConnection;
        
        try
        {
            using var timeoutCancellationTokenSource = new CancellationTokenSource(networkConnection.TimeoutDuration);
            using var combinedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancellationTokenSource.Token, cancellationToken);
            
            await _tcpClient.ConnectAsync(networkConnection.Uri.Host, networkConnection.Uri.Port, combinedCancellationTokenSource.Token);
            
            if (_tcpClient.Connected)
            {
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

                SendPacketToNetwork(new PacketData($"{NetworkConstants.MinerLoginType}|{networkConnection.WalletAddress}", true, (packet, _) =>
                {
                    if (Encoding.UTF8.GetString(packet).Equals(NetworkConstants.SendLoginAccepted))
                    {
                        Ready?.Invoke();
                        GetNewBlockHeader();
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

        Disconnected?.Invoke(reason);
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

                    using var networkTimeoutCts = new CancellationTokenSource(_networkConnection!.TimeoutDuration);

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
    
    private void GetNewBlockHeader()
    {
        SendPacketToNetwork(new PacketData(NetworkConstants.ReceiveAskCurrentBlockMining, true, ReceiveBlockHeaderPacketHandler));
    }
    
    private void ReceiveBlockHeaderPacketHandler(ReadOnlySpan<byte> packet, TimeSpan roundTripTime)
    {
        var currentBlockIndication = _blockHeader.BlockIndication;

        if (!_blockHeader.UpdateBlockHeader(packet))
        {
            GetNewBlockHeader();
            return;
        }

        if (currentBlockIndication == _blockHeader.BlockIndication)
        {
            GetNewBlockHeader();
            return;
        }

        SendPacketToNetwork(new PacketData($"{NetworkConstants.ReceiveAskContentBlockMethod}|{_blockHeader.BlockMethod}", true, ReceiveBlockMethodPacketHandler));
    }

    private void ReceiveBlockMethodPacketHandler(ReadOnlySpan<byte> packet, TimeSpan roundTripTime)
    {
        if (_blockHeader.UpdateBlockMethod(packet))
        {
            HasNewBlock?.Invoke(_blockHeader);
        }

        GetNewBlockHeader();
    }

    public void Dispose()
    {
        InternalDisconnect();
        _connectionSemaphoreSlim.Dispose();
    }
}