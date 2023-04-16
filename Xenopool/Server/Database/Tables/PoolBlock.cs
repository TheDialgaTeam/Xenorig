using System.ComponentModel.DataAnnotations;

namespace Xenopool.Server.Database.Tables;

public sealed class PoolBlock
{
    [Key]
    public long Height { get; set; }

    public string MinerAddress { get; set; } = string.Empty;
}