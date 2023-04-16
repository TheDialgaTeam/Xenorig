using Google.Protobuf;
using Microsoft.Extensions.Options;
using Xenolib.Algorithms.Xenophyte.Centralized.Networking;
using Xenopool.Server.Networking.Pool;
using Xenopool.Server.Options;

namespace Xenopool.Server.Networking.SoloMining;

public sealed class SoloMiningNetwork : IDisposable
{
    public BlockHeaderResponse? BlockHeaderResponse { get; private set; }
    
    private readonly ILogger<SoloMiningNetwork> _logger;
    
    private readonly Network _network = new();
    private readonly NetworkConnection _networkConnection;
    
    public SoloMiningNetwork(IOptions<XenopoolOptions> options, ILogger<SoloMiningNetwork> logger, IHostApplicationLifetime hostApplicationLifetime)
    {
        _logger = logger;

        _networkConnection = new NetworkConnection
        {
            Uri = new UriBuilder(options.Value.SoloMining.Host) { Port = options.Value.SoloMining.Port }.Uri, 
            WalletAddress = options.Value.RpcWallet.WalletAddress, 
            TimeoutDuration = TimeSpan.FromSeconds(options.Value.SoloMining.NetworkTimeoutDuration)
        };

        hostApplicationLifetime.ApplicationStopping.Register(() => StopAsync().Wait());
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _network.Disconnected += NetworkOnDisconnected;
        _network.Ready += NetworkOnReady;
        _network.HasNewBlock += NetworkOnHasNewBlock;
        
        await _network.ConnectAsync(_networkConnection, cancellationToken);
    }
    
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _network.Disconnected -= NetworkOnDisconnected;
        _network.Ready -= NetworkOnReady;
        _network.HasNewBlock -= NetworkOnHasNewBlock;
        
        await _network.DisconnectAsync(cancellationToken);
    }
    
    private async void NetworkOnDisconnected(string reason)
    {
        Logger.PrintDisconnected(_logger, _networkConnection.Uri.Host, reason);
        await _network.ConnectAsync(_networkConnection);
    }
    
    private void NetworkOnReady()
    {
        Logger.PrintConnected(_logger, "SOLO", _networkConnection.Uri.Host);
    }
    
    private void NetworkOnHasNewBlock(BlockHeader blockHeader)
    {
        Logger.PrintJob(_logger, "new job", _networkConnection.Uri.Host, blockHeader.BlockDifficulty, blockHeader.BlockMethod, blockHeader.BlockHeight);

        BlockHeaderResponse = new BlockHeaderResponse
        {
            BlockHeight = blockHeader.BlockHeight,
            BlockTimestampCreate = blockHeader.BlockTimestampCreate,
            BlockMethod = blockHeader.BlockMethod,
            BlockIndication = blockHeader.BlockIndication,
            BlockDifficulty = blockHeader.BlockDifficulty,
            BlockMinRange = blockHeader.BlockMinRange,
            BlockMaxRange = blockHeader.BlockMaxRange,
            XorKey = ByteString.CopyFrom(blockHeader.XorKey),
            AesKey = ByteString.CopyFrom(blockHeader.AesKey),
            AesIv = ByteString.CopyFrom(blockHeader.AesIv),
            AesRound = blockHeader.AesRound
        };
    }

    public void Dispose()
    {
        _network.Dispose();
    }
}