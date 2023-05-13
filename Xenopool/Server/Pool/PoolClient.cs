using Xenopool.Server.Database.Tables;

namespace Xenopool.Server.Pool;

public sealed class PoolClient
{
    private readonly PoolAccount _poolAccount;
    private readonly string _workerId;
        
    private DateTime _lastRequestTime = DateTime.Now;

    private ulong _sharePoints = 0;

    public PoolClient(PoolAccount poolAccount, string workerId)
    {
        _poolAccount = poolAccount;
        _workerId = workerId;
    }

    public bool IsExpire()
    {
        return (DateTime.Now - _lastRequestTime).TotalMinutes > 1;
    }

    public void Ping()
    {
        _lastRequestTime = DateTime.Now;
    }
}