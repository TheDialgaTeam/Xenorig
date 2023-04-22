using Microsoft.EntityFrameworkCore;
using Xenopool.Server.Database.Tables;

namespace Xenopool.Server.Database.Repository;

public sealed class PoolAccountRepository : IDisposable
{
    private readonly SqliteDatabaseContext _context;

    public PoolAccountRepository(SqliteDatabaseContext context)
    {
        _context = context;
    }

    public async Task<PoolAccount> GetOrCreateAccountAsync(string walletAddress, CancellationToken cancellationToken)
    {
        var count = await _context.PoolAccounts.CountAsync(account => account.WalletAddress == walletAddress, cancellationToken);

        if (count == 0)
        {
            var account = new PoolAccount { WalletAddress = walletAddress };
            _context.PoolAccounts.Add(account);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return await _context.PoolAccounts.SingleAsync(account => account.WalletAddress == walletAddress, cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}