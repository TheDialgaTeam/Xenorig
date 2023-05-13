using System.ComponentModel.DataAnnotations;

namespace Xenopool.Server.Database.Tables;

public sealed class PoolShare
{
    [Key]
    public string WalletAddress { get; set; } = string.Empty;
    
    public string WorkerId { get; set; } = string.Empty;
    
    public long Height { get; set; }
    
    public long SharePoints { get; set; }
}