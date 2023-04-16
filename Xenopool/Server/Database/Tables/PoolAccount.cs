using System.ComponentModel.DataAnnotations;

namespace Xenopool.Server.Database.Tables;

public sealed class PoolAccount
{
    [Key]
    public string WalletAddress { get; set; } = string.Empty;
    
    public ulong PayoutAmount { get; set; }
    
    public ulong MinimumPayoutAmount { get; set; }
    
    public bool IsBanned { get; set; }
    
    public string? BanReason { get; set; }
}