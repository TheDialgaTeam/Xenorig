using Microsoft.EntityFrameworkCore;
using Xenolib.Algorithms.Xenophyte.Centralized.Networking.Pool;
using Xenolib.Utilities.Buffer;
using Xenopool.Server.Database;
using Xenopool.Server.Database.Repository;

namespace Xenopool.Server.Pool;

public sealed class PoolClientCollection : IDisposable
{
    public Dictionary<string, PoolClient> PoolClients { get; } = new();

    public Dictionary<string, PoolClient.Worker> PoolWorkers { get; } = new();

    private readonly IDbContextFactory<SqliteDatabaseContext> _contextFactory;

    private readonly Timer _poolWorkerChecker;
    
    public PoolClientCollection(IDbContextFactory<SqliteDatabaseContext> contextFactory)
    {
        _contextFactory = contextFactory;
        _poolWorkerChecker = new Timer(CheckPoolWorkerValidity, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }
    
    public async Task<LoginResponse> RegisterClientAsync(LoginRequest request)
    {
        if (!PoolClients.TryGetValue(request.WalletAddress, out var poolClient))
        {
            poolClient = new PoolClient(new PoolAccountRepository(await _contextFactory.CreateDbContextAsync()), request.WalletAddress);
            PoolClients.Add(request.WalletAddress, poolClient);
        }

        if (poolClient.Account.IsBanned)
        {
            return new LoginResponse { Status = false, Reason = poolClient.Account.BanReason ?? string.Empty };
        }

        string guid;

        do
        {
            guid = Guid.NewGuid().ToString();
        } while (!PoolWorkers.TryAdd(guid, new PoolClient.Worker(poolClient.Account, request.WorkerId)));
                
        return new LoginResponse { Status = true, Token = guid };
    }

    private void CheckPoolWorkerValidity(object? state)
    {
        using var expiredPoolWorker = ArrayPoolOwner<string>.Rent(PoolWorkers.Count);
        var removedCount = 0;

        foreach (var poolWorker in PoolWorkers)
        {
            if (poolWorker.Value.IsExpire())
            {
                expiredPoolWorker.Span[removedCount++] = poolWorker.Key;
            }
        }

        for (var i = 0; i < removedCount; i++)
        {
            PoolWorkers.Remove(expiredPoolWorker.Span[i]);
        }
    }

    public void Dispose()
    {
        _poolWorkerChecker.Dispose();
    }
}