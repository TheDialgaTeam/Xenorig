using Grpc.Core;
using Xenolib.Algorithms.Xenophyte.Centralized.Networking.Pool;
using Xenopool.Server.SoloMining;

namespace Xenopool.Server.Pool;

public sealed class PoolService : Xenolib.Algorithms.Xenophyte.Centralized.Networking.Pool.Pool.PoolBase
{
    private readonly SoloMiningNetwork _soloMiningNetwork;
    private readonly PoolClientManager _poolClientManager;

    public PoolService(SoloMiningNetwork soloMiningNetwork, PoolClientManager poolClientManager)
    {
        _soloMiningNetwork = soloMiningNetwork;
        _poolClientManager = poolClientManager;
    }

    public override async Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
    {
        return await _poolClientManager.AddClientAsync(request, context);
    }

    public override async Task<BlockHeaderResponse> GetBlockHeader(BlockHeaderRequest request, ServerCallContext context)
    {
        var poolClient = await _poolClientManager.GetClientAsync(request.Token, context.CancellationToken);

        if (poolClient == null)
        {
            return new BlockHeaderResponse { Status = false, Reason = "User not authorized." };
        }
        
        poolClient.Ping();
        
        return await Task.FromResult(_soloMiningNetwork.BlockHeaderResponse);
    }

    public override async Task<JobHeaderResponse> RequestNewJob(JobHeaderRequest request, ServerCallContext context)
    {
        var poolClient = await _poolClientManager.GetClientAsync(request.Token, context.CancellationToken);

        if (poolClient == null)
        {
            return new JobHeaderResponse { Status = false, Reason = "User not authorized." };
        }
        
        poolClient.Ping();
        
        return await base.RequestNewJob(request, context);
    }

    public override Task<JobSubmitResponse> SubmitJob(JobSubmitRequest request, ServerCallContext context)
    {
        return base.SubmitJob(request, context);
    }
}