namespace Xenopool.Server.Pool;

public sealed class PoolShare
{
    public required long BlockHeight { get; init; }
    
    public required long FirstNumber { get; init; }
    
    public required long SecondNumber { get; init; }
    
    public required char Operator { get; init; }
    
    public required long Solution { get; init; }

    public required string EncryptedShare { get; init; }
    
    public required string EncryptedShareHash { get; init; }
}