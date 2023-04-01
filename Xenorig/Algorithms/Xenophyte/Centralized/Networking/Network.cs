using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xenorig.Options;
using Xenorig.Utilities;
using Xenorig.Utilities.Buffer;
using Xenorig.Utilities.KeyDerivationFunction;

namespace Xenorig.Algorithms.Xenophyte.Centralized.Networking;

public delegate void NetworkStatus(bool isConnected, string reason = "");

public sealed partial class Network : IDisposable
{
    public event NetworkStatus? Status;

    public event Action? Ready;

    private static readonly byte[] CertificateSupportedCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789&~#@\'(\\)="u8.ToArray();

    private readonly XenorigOptions _options;
    private readonly Pool _pool;
    
    private TcpClient? _tcpClient;

    private readonly SemaphoreSlim _connectionSemaphoreSlim = new(1, 1);

    private readonly byte[] _networkAesKey = new byte[NetworkConstants.MajorUpdate1SecurityCertificateSizeItem / 8];
    private readonly byte[] _networkAesIv = new byte[16];

    private BlockingCollection<PacketData>? _packetDataBlockingCollection;
    private Task? _networkPacketHandlerTask;

    public Network(XenorigOptions options, Pool pool)
    {
        _options = options;
        _pool = pool;
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

    [GeneratedRegex("(?<hostname>.+):?(?<port>\\d+)?$")]
    private static partial Regex GetHostnameAndPortRegex();

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
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

    private async Task InternalConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_tcpClient != null) return;
            
        _tcpClient = new TcpClient();

        using var networkTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.NetworkTimeoutDuration));
        using var connectionTimeoutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, networkTimeoutCts.Token);

        try
        {
            if (IPEndPoint.TryParse(_pool.Url, out var ipEndPoint))
            {
                await _tcpClient.ConnectAsync(ipEndPoint.Address, ipEndPoint.Port == 0 ? NetworkConstants.SeedNodePort : ipEndPoint.Port, connectionTimeoutTokenSource.Token);
            }
            else
            {
                var hostAndPortMatch = GetHostnameAndPortRegex().Match(_pool.Url);
                var host = await Dns.GetHostAddressesAsync(hostAndPortMatch.Groups["hostname"].Value, connectionTimeoutTokenSource.Token);
                await _tcpClient.ConnectAsync(host, hostAndPortMatch.Groups.ContainsKey("port") ? int.Parse(hostAndPortMatch.Groups["port"].Value) : NetworkConstants.SeedNodePort, connectionTimeoutTokenSource.Token);
            }

            if (_tcpClient.Connected)
            {
                Status?.Invoke(true);
                
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

                SendPacketToNetwork(new PacketData($"{NetworkConstants.MinerLoginType}|{_pool.Username}", true, (packet, _) =>
                {
                    if (Encoding.UTF8.GetString(packet).Equals(NetworkConstants.SendLoginAccepted))
                    {
                        Ready?.Invoke();
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
        
        Status?.Invoke(false, reason);
    }

    private async Task NetworkPacketHandlerTask()
    {
        while (_packetDataBlockingCollection is { IsAddingCompleted: false })
        {
            try
            {
                using var packetData = _packetDataBlockingCollection.Take();
                Debug.Write(packetData.ToString());
                
                try
                {
                    if (_tcpClient == null) return;

                    using var networkTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.NetworkTimeoutDuration));

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
    
    public void Dispose()
    {
        InternalDisconnect();
        _connectionSemaphoreSlim.Dispose();
    }
}