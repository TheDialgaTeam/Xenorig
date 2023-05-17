using Xenopool.Server.Database.Tables;

namespace Xenopool.Server.Database.Repository;

public sealed class PoolAccountRepository : IDisposable, IAsyncDisposable
{
    private readonly SqliteDatabaseContext _context;

    public PoolAccountRepository(SqliteDatabaseContext context)
    {
        _context = context;
    }

    public PoolAccount CreateAccount(string walletAddress)
    {
        var account = new PoolAccount { WalletAddress = walletAddress };
        _context.PoolAccounts.Add(account);
        return account;
    }
    
    public PoolAccount? GetAccount(string walletAddress)
    {
        return _context.PoolAccounts.SingleOrDefault(account => account.WalletAddress == walletAddress);
    }

    public void Dispose()
    {
        _context.SaveChanges();
        _context.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        _context.SaveChanges();
        return _context.DisposeAsync();
    }
}