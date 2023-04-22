using Xenopool.Server.Database.Tables;

namespace Xenopool.Server.Pool;

public sealed class PoolClient
{
    public string WorkerId { get; }
    
    private readonly PoolAccount _poolAccount;

    public PoolClient(PoolAccount poolAccount, string workerId)
    {
        WorkerId = workerId;
        _poolAccount = poolAccount;
    }
}