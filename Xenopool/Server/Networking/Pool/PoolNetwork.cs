using Grpc.Core;
using Xenopool.Server.Networking.SoloMining;

namespace Xenopool.Server.Networking.Pool;

public sealed class PoolNetwork : Pool.PoolBase
{
    private readonly SoloMiningNetwork _soloMiningNetwork;

    public PoolNetwork(SoloMiningNetwork soloMiningNetwork)
    {
        _soloMiningNetwork = soloMiningNetwork;
    }

    public override Task<BlockHeaderResponse> GetBlockHeader(BlockHeaderRequest request, ServerCallContext context)
    {
        return Task.FromResult(_soloMiningNetwork.BlockHeaderResponse);
    }
}