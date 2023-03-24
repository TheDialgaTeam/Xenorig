using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xenorig.Options;
using Xenorig.Utilities;
using Xenorig.Utilities.Buffer;
using Xenorig.Utilities.KeyDerivationFunction;

namespace Xenorig.Algorithms.Xenophyte.Centralized.Networking;

public sealed partial class Network : IDisposable
{
    public event Action? IsReconnectFailed;

    private static readonly byte[] CertificateSupportedCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789&~#@\'(\\)="u8.ToArray();

    private readonly XenorigOptions _options;

    private int _isNetworkConnected;

    private readonly Pool _pool;
    private int _poolRetryCount;

    private TcpClient? _tcpClient;

    private readonly SemaphoreSlim _connectionSemaphoreSlim = new(1, 1);
    private readonly SemaphoreSlim _networkSemaphoreSlim = new(1, 1);

    private readonly byte[] _networkAesKey = new byte[NetworkConstants.MajorUpdate1SecurityCertificateSizeItem / 8];
    private readonly byte[] _networkAesIv = new byte[16];

    public Network(XenorigOptions options, Pool pool)
    {
        _options = options;
        _pool = pool;
    }

    private static ArrayOwner<byte> GenerateCertificate()
    {
        var requestedLength = Encoding.UTF8.GetByteCount(NetworkConstants.NetworkGenesisSecondaryKey) + NetworkConstants.MajorUpdate1SecurityCertificateSizeItem;
        var outputArrayOwner = ArrayOwner<byte>.Rent(requestedLength);
        var output = outputArrayOwner.Span;

        var index = Encoding.UTF8.GetBytes(NetworkConstants.NetworkGenesisSecondaryKey, output);
        var upperbound = CertificateSupportedCharacters.Length - 1;

        for (var i = NetworkConstants.MajorUpdate1SecurityCertificateSizeItem - 1; i >= 0; i--)
        {
            output.GetRef(index + i) = CertificateSupportedCharacters.GetRef(RandomNumberGeneratorUtility.GetRandomBetween(0, upperbound));
        }
        
        return outputArrayOwner;
    }

    [GeneratedRegex("(?<hostname>.+):?(?<port>\\d+)?$")]
    private static partial Regex GetHostnameAndPortRegex();

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (_isNetworkConnected == 1) return;

        try
        {
            await _connectionSemaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);

            do
            {
                _tcpClient?.Dispose();
                _tcpClient = new TcpClient();
                _tcpClient.NoDelay = true;

                using (var networkTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.NetworkTimeoutDuration)))
                using (var connectionTimeoutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, networkTimeoutCts.Token))
                {
                    try
                    {
                        if (IPEndPoint.TryParse(_pool.Url, out var ipEndPoint))
                        {
                            await _tcpClient.ConnectAsync(ipEndPoint.Address, ipEndPoint.Port == 0 ? NetworkConstants.SeedNodePort : ipEndPoint.Port, connectionTimeoutTokenSource.Token).ConfigureAwait(false);
                        }
                        else
                        {
                            var hostAndPortMatch = GetHostnameAndPortRegex().Match(_pool.Url);
                            var host = await Dns.GetHostAddressesAsync(hostAndPortMatch.Groups["hostname"].Value, connectionTimeoutTokenSource.Token);
                            await _tcpClient.ConnectAsync(host, hostAndPortMatch.Groups.ContainsKey("port") ? int.Parse(hostAndPortMatch.Groups["port"].Value) : NetworkConstants.SeedNodePort, connectionTimeoutTokenSource.Token).ConfigureAwait(false);
                        }
                    }
                    catch
                    {
                        // Connection Failed, try again.
                    }
                }

                if (!_tcpClient.Connected) continue;

                // Connected
                _poolRetryCount = 0;
                Interlocked.Exchange(ref _isNetworkConnected, 1);
                DoConnectionCertificate();
            } while (_isNetworkConnected == 0 && !cancellationToken.IsCancellationRequested && ++_poolRetryCount < _options.MaxRetryCount);

            if (_isNetworkConnected == 0)
            {
                IsReconnectFailed?.Invoke();
            }
        }
        finally
        {
            _connectionSemaphoreSlim.Release();
        }
    }
    
    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        if (_isNetworkConnected == 0) return;

        try
        {
            await _connectionSemaphoreSlim.WaitAsync(cancellationToken);

            _tcpClient?.Dispose();
            _tcpClient = null;

            // Disconnected
            Interlocked.Exchange(ref _isNetworkConnected, 0);
        }
        finally
        {
            _connectionSemaphoreSlim.Release();
        }
    }

    public async void SendPacketToNetwork(PacketData packetData)
    {
        try
        {
            await _networkSemaphoreSlim.WaitAsync();

            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(_options.NetworkTimeoutDuration));
            var executeTask = Task.Run(() =>
            {
                using var packet = packetData;
                return _tcpClient != null && packet.Execute(_tcpClient.GetStream(), _networkAesKey, _networkAesIv);
            });

            var completedTask = await Task.WhenAny(timeoutTask, executeTask);

            if (completedTask == timeoutTask)
            {
                // Send and Receive Timeout, reconnect.
                await ReconnectAsync(CancellationToken.None);
                return;
            }

            if (await executeTask) return;

            // Execute failed, reconnect.
            await ReconnectAsync(CancellationToken.None);
        }
        catch
        {
            await ReconnectAsync(CancellationToken.None);
        }
        finally
        {
            _networkSemaphoreSlim.Release();
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
        var certificateArrayPoolOwner = GenerateCertificate();
        var certificate = certificateArrayPoolOwner.Span;

        var saltHex = Convert.ToHexString(certificate[..8]);
        Span<byte> salt = stackalloc byte[Encoding.UTF8.GetByteCount(saltHex)];
        Encoding.UTF8.GetBytes(saltHex, salt);

        using (var pbkdf1 = new PBKDF1(certificate, salt))
        {
            pbkdf1.FillBytes(_networkAesKey);
            pbkdf1.FillBytes(_networkAesIv);
        }

        SendPacketToNetwork(new PacketData(certificateArrayPoolOwner, false));

        SendPacketToNetwork(new PacketData($"{NetworkConstants.MinerLoginType}|{_pool.Username}", true, (packet, time) =>
        {
            if (!Encoding.UTF8.GetString(packet).Equals(NetworkConstants.SendLoginAccepted))
            {
                // Login failed, reconnect.
                ReconnectAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
        }));
    }

    public void Dispose()
    {
        _tcpClient?.Dispose();
        _connectionSemaphoreSlim.Dispose();
        _networkSemaphoreSlim.Dispose();
    }
}