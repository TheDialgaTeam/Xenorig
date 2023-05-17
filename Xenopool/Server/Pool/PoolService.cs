using Grpc.Core;
using Xenolib.Algorithms.Xenophyte.Centralized.Networking.Pool;
using Xenopool.Server.SoloMining;

namespace Xenopool.Server.Pool;

public sealed class PoolService : Xenolib.Algorithms.Xenophyte.Centralized.Networking.Pool.PoolService.PoolServiceBase
{
    private readonly SoloMiningNetwork _soloMiningNetwork;
    private readonly PoolClientManager _poolClientManager;

    public PoolService(SoloMiningNetwork soloMiningNetwork, PoolClientManager poolClientManager)
    {
        _soloMiningNetwork = soloMiningNetwork;
        _poolClientManager = poolClientManager;
    }

    public override Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
    {
        return Task.FromResult(_poolClientManager.AddClient(request));
    }

    public override Task<BlockHeaderResponse> GetBlockHeader(BlockHeaderRequest request, ServerCallContext context)
    {
        if (!_poolClientManager.TryGetClient(request.Token, out var poolClient))
        {
            return Task.FromResult(new BlockHeaderResponse { Status = false, Reason = "User not authorized." });
        }

        poolClient.Ping();

        return Task.FromResult(_soloMiningNetwork.CurrentMiningJob == null ? new BlockHeaderResponse { Status = false, Reason = "Blockchain is not ready." } : poolClient.GetBlockHeader(_soloMiningNetwork.CurrentMiningJob));
    }

    public override Task<JobSubmitResponse> SubmitJob(JobSubmitRequest request, ServerCallContext context)
    {
        if (!_poolClientManager.TryGetClient(request.Token, out var poolClient))
        {
            return Task.FromResult(new JobSubmitResponse { Status = false, Reason = "User not authorized." });
        }

        return base.SubmitJob(request, context);
    }
}