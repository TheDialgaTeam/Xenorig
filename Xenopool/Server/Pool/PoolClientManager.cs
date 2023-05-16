using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Xenolib.Algorithms.Xenophyte.Centralized.Networking.Pool;
using Xenopool.Server.Database;
using Xenopool.Server.Database.Repository;

namespace Xenopool.Server.Pool;

public sealed class PoolClientManager : IDisposable
{
    private readonly PoolAccountRepository _poolAccountRepository;
    private readonly Timer _poolClientValidityCheck;
    
    private readonly ConcurrentDictionary<string, PoolClient> _poolClients = new();

    public PoolClientManager(IDbContextFactory<SqliteDatabaseContext> contextFactory)
    {
        _poolAccountRepository = new PoolAccountRepository(contextFactory.CreateDbContext());
        _poolClientValidityCheck = new Timer(CheckClientValidity, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public LoginResponse AddClient(LoginRequest request)
    {
        var account = _poolAccountRepository.GetAccount(request.WalletAddress) ?? _poolAccountRepository.CreateAccount(request.WalletAddress);

        if (account.IsBanned)
        {
            return new LoginResponse { Status = false, Reason = account.BanReason ?? string.Empty };
        }
        
        string guid;

        do
        {
            guid = Guid.NewGuid().ToString();
        } while (!_poolClients.TryAdd(guid, new PoolClient(account, request.WorkerId)));

        return new LoginResponse { Status = true, Token = guid };
    }
    
    public bool TryGetClient(string token, [MaybeNullWhen(false)] out PoolClient poolClient)
    {
        return _poolClients.TryGetValue(token, out poolClient);
    }

    private void CheckClientValidity(object? _)
    {
        foreach (var poolClient in _poolClients)
        {
            if (poolClient.Value.IsExpire())
            {
                _poolClients.TryRemove(poolClient);
            }
        }
    }

    public void Dispose()
    {
        _poolAccountRepository.Dispose();
        _poolClientValidityCheck.Dispose();
    }
}