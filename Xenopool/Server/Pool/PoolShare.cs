namespace Xenopool.Server.Pool;

public struct PoolShare
{
    public long BlockHeight { get; init; }
    
    public long FirstNumber { get; init; }
    
    public long SecondNumber { get; init; }
    
    public char Operator { get; init; }
    
    public long Solution { get; init; }

    public string EncryptedShare { get; init; }
    
    public string EncryptedShareHash { get; init; }
}