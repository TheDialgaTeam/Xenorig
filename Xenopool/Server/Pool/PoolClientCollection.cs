using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Xenolib.Algorithms.Xenophyte.Centralized.Networking.Pool;
using Xenopool.Server.Database;
using Xenopool.Server.Database.Repository;

namespace Xenopool.Server.Pool;

public sealed class PoolClientCollection
{
    public Dictionary<string, PoolClient> PoolClients { get; } = new();
    
    private readonly IDbContextFactory<SqliteDatabaseContext> _contextFactory;

    public PoolClientCollection(IDbContextFactory<SqliteDatabaseContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<LoginResponse> RegisterClient(LoginRequest request, ServerCallContext context)
    {
        try
        {
            await using var dbContext = await _contextFactory.CreateDbContextAsync(context.CancellationToken);
            using var accountRepository = new PoolAccountRepository(dbContext);

            var account = await accountRepository.GetOrCreateAccountAsync(request.WalletAddress, context.CancellationToken);
            var guid = Guid.NewGuid();

            PoolClients.Add(guid.ToString(), new PoolClient(account, request.WorkerId));
            
            return new LoginResponse { Result = true, Token = guid.ToString() };
        }
        catch (OperationCanceledException)
        {
            return new LoginResponse { Result = false, Token = string.Empty, Reason = "Login Timeout." };
        }
        catch
        {
            return new LoginResponse { Result = false, Token = string.Empty, Reason = "Unexpected error occured." };
        }
    }
}