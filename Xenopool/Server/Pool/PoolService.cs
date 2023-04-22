using Grpc.Core;
using Xenolib.Algorithms.Xenophyte.Centralized.Networking.Pool;
using Xenopool.Server.SoloMining;

namespace Xenopool.Server.Pool;

public sealed class PoolService : Xenolib.Algorithms.Xenophyte.Centralized.Networking.Pool.Pool.PoolBase
{
    private readonly SoloMiningNetwork _soloMiningNetwork;
    private readonly PoolClientCollection _poolClientCollection;

    public PoolService(SoloMiningNetwork soloMiningNetwork, PoolClientCollection poolClientCollection)
    {
        _soloMiningNetwork = soloMiningNetwork;
        _poolClientCollection = poolClientCollection;
    }

    public override async Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
    {
        return await _poolClientCollection.RegisterClient(request, context);
    }

    public override async Task<BlockHeaderResponse> GetBlockHeader(BlockHeaderRequest request, ServerCallContext context)
    {
        while (_soloMiningNetwork.BlockHeaderResponse == null)
        {
            await Task.Delay(1, context.CancellationToken);
        }

        return _soloMiningNetwork.BlockHeaderResponse;
    }
}