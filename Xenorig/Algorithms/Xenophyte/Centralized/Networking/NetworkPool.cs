using Microsoft.Extensions.Logging;
using Xenolib.Algorithms.Xenophyte.Centralized.Networking;
using Xenorig.Options;

namespace Xenorig.Algorithms.Xenophyte.Centralized.Networking;

public sealed class NetworkPool : IDisposable
{
    public event Action? HasNewBlock;

    public event Action? ConnectionFailed;

    public BlockHeader BlockHeader { get; } = new();

    private readonly ILogger _logger;
    private readonly XenorigOptions _options;

    private int _isNetworkActive;

    private readonly Pool _pool;
    private int _poolRetryCount;

    private readonly Network _getBlockHeaderNetwork;
    private readonly Network _sentBlockNetwork;

    public NetworkPool(ILogger logger, XenorigOptions options, Pool pool)
    {
        _logger = logger;
        _options = options;
        _pool = pool;

        _getBlockHeaderNetwork = new Network(options, pool);
        _sentBlockNetwork = new Network(options, pool);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _isNetworkActive, 1, 0) == 1)
        {
            return;
        }

        _getBlockHeaderNetwork.Status += GetBlockHeaderNetworkOnStatus;
        _getBlockHeaderNetwork.Ready += GetBlockHeaderNetworkOnReady;
        _sentBlockNetwork.Status += SentBlockNetworkOnStatus;

        var tasks = new[]
        {
            _getBlockHeaderNetwork.ConnectAsync(cancellationToken),
            _sentBlockNetwork.ConnectAsync(cancellationToken)
        };

        await Task.WhenAll(tasks);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _isNetworkActive, 0, 1) == 0)
        {
            return;
        }

        var tasks = new[]
        {
            _getBlockHeaderNetwork.DisconnectAsync(cancellationToken),
            _sentBlockNetwork.DisconnectAsync(cancellationToken)
        };

        await Task.WhenAll(tasks);

        _getBlockHeaderNetwork.Status -= GetBlockHeaderNetworkOnStatus;
        _getBlockHeaderNetwork.Ready -= GetBlockHeaderNetworkOnReady;
        _sentBlockNetwork.Status -= SentBlockNetworkOnStatus;
    }

    public void SendPacketToNetwork(PacketData packetData)
    {
        _getBlockHeaderNetwork.SendPacketToNetwork(packetData);
    }

    private async void GetBlockHeaderNetworkOnStatus(bool isConnected, string reason)
    {
        if (_isNetworkActive == 1)
        {
            if (!isConnected)
            {
                Logger.PrintDisconnected(_logger, _pool.Url, reason == string.Empty ? "None" : reason);

                if (_poolRetryCount++ < _options.MaxRetryCount)
                {
                    await _getBlockHeaderNetwork.ConnectAsync(CancellationToken.None);
                }
                else
                {
                    await StopAsync();
                    ConnectionFailed?.Invoke();
                }
            }
            else
            {
                _poolRetryCount = 0;
            }
        }
    }

    private void GetBlockHeaderNetworkOnReady()
    {
        Logger.PrintConnected(_logger, "SOLO", _pool.Url);
        GetNewBlockHeader();
    }

    private async void SentBlockNetworkOnStatus(bool isConnected, string reason)
    {
        if (_isNetworkActive == 1)
        {
            if (!isConnected)
            {
                await _sentBlockNetwork.ConnectAsync(CancellationToken.None);
            }
        }
    }

    private void GetNewBlockHeader()
    {
        _getBlockHeaderNetwork.SendPacketToNetwork(new PacketData(NetworkConstants.ReceiveAskCurrentBlockMining, true, ReceiveBlockHeaderPacketHandler));
    }

    private void ReceiveBlockHeaderPacketHandler(ReadOnlySpan<byte> packet, TimeSpan roundTripTime)
    {
        var currentBlockIndication = BlockHeader.BlockIndication;

        if (!BlockHeader.UpdateBlockHeader(packet))
        {
            GetNewBlockHeader();
            return;
        }

        if (currentBlockIndication == BlockHeader.BlockIndication)
        {
            GetNewBlockHeader();
            return;
        }

        _getBlockHeaderNetwork.SendPacketToNetwork(new PacketData($"{NetworkConstants.ReceiveAskContentBlockMethod}|{BlockHeader.BlockMethod}", true, ReceiveBlockMethodPacketHandler));
    }

    private void ReceiveBlockMethodPacketHandler(ReadOnlySpan<byte> packet, TimeSpan roundTripTime)
    {
        if (BlockHeader.UpdateBlockMethod(packet))
        {
            HasNewBlock?.Invoke();
        }

        GetNewBlockHeader();
    }

    public void Dispose()
    {
        _getBlockHeaderNetwork.Dispose();
        _sentBlockNetwork.Dispose();
    }
}