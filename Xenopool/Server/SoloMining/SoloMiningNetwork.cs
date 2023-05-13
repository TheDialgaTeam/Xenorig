using Google.Protobuf;
using Microsoft.Extensions.Options;
using Xenolib.Algorithms.Xenophyte.Centralized.Networking.Pool;
using Xenolib.Algorithms.Xenophyte.Centralized.Networking.Solo;
using Xenolib.Algorithms.Xenophyte.Centralized.Utilities;
using Xenopool.Server.Options;
using BlockHeader = Xenolib.Algorithms.Xenophyte.Centralized.Networking.Solo.BlockHeader;

namespace Xenopool.Server.SoloMining;

public sealed class SoloMiningNetwork : IDisposable
{
    public BlockHeaderResponse BlockHeaderResponse { get; private set; } = new() { Status = false, Reason = "Blockchain is not ready." };

    public Span<long> EasyBlockValues => _easyBlockValues.AsSpan(0, _easyBlockValuesLength);

    private readonly ILogger<SoloMiningNetwork> _logger;

    private readonly Network _network = new();
    private readonly NetworkConnection _networkConnection;

    private readonly long[] _easyBlockValues = new long[256];
    private int _easyBlockValuesLength = 256;

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

    private async Task StopAsync(CancellationToken cancellationToken = default)
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
            Status = true,
            Header = new Xenolib.Algorithms.Xenophyte.Centralized.Networking.Pool.BlockHeader
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
            }
        };
        
        _easyBlockValuesLength = CpuMinerUtility.GenerateEasyBlockNumbers(blockHeader.BlockMinRange, blockHeader.BlockMaxRange, _easyBlockValues);
    }

    public void Dispose()
    {
        _network.Dispose();
    }
}