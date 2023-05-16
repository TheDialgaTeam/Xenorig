#pragma warning disable IL2026

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xenopool.Server.Database.Tables;

namespace Xenopool.Server.Database;

public sealed class SqliteDatabaseContext : DbContext
{
    public DbSet<PoolAccount> PoolAccounts { get; set; } = null!;

    public SqliteDatabaseContext(DbContextOptions<SqliteDatabaseContext> options) : base(options)
    {
    }
}