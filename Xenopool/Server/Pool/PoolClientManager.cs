using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Xenolib.Algorithms.Xenophyte.Centralized.Networking.Pool;
using Xenolib.Utilities.Buffer;
using Xenopool.Server.Database;
using Xenopool.Server.Database.Repository;

namespace Xenopool.Server.Pool;

public sealed class PoolClientManager : IDisposable
{
    private readonly PoolAccountRepository _poolAccountRepository;
    private readonly Timer _poolClientValidityCheck;

    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    private readonly Dictionary<string, PoolClient> _poolClients = new();

    public PoolClientManager(IDbContextFactory<SqliteDatabaseContext> contextFactory)
    {
        _poolAccountRepository = new PoolAccountRepository(contextFactory.CreateDbContext());
        _poolClientValidityCheck = new Timer(CheckClientValidity, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public async Task<LoginResponse> AddClientAsync(LoginRequest request, ServerCallContext context)
    {
        try
        {
            await _semaphoreSlim.WaitAsync(context.CancellationToken);
        
            var account = await _poolAccountRepository.GetAccountAsync(request.WalletAddress, context.CancellationToken) ?? _poolAccountRepository.CreateAccount(request.WalletAddress);

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
        finally
        {
            if (_semaphoreSlim.CurrentCount == 0) _semaphoreSlim.Release();
        }
    }

    public async Task<PoolClient?> GetClientAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            await _semaphoreSlim.WaitAsync(cancellationToken);
            return _poolClients.TryGetValue(token, out var poolClient) ? poolClient : null;
        }
        finally
        {
            if (_semaphoreSlim.CurrentCount == 0) _semaphoreSlim.Release();
        }
    }

    private void CheckClientValidity(object? _)
    {
        try
        {
            _semaphoreSlim.Wait();
            
            using var expiredPoolClients = ArrayPoolOwner<string>.Rent(_poolClients.Count);
            var expiredPoolClientCount = 0;
            
            foreach (var poolClient in _poolClients)
            {
                if (poolClient.Value.IsExpire())
                {
                    expiredPoolClients.Span[expiredPoolClientCount++] = poolClient.Key;
                }
            }

            for (var i = 0; i < expiredPoolClientCount; i++)
            {
                _poolClients.Remove(expiredPoolClients.Span[i]);
            }
        }
        finally
        {
            if (_semaphoreSlim.CurrentCount == 0) _semaphoreSlim.Release();
        }
    }

    public void Dispose()
    {
        _poolAccountRepository.Dispose();
        _poolClientValidityCheck.Dispose();
        _semaphoreSlim.Dispose();
    }
}