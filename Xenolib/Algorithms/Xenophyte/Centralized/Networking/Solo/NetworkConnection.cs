namespace Xenolib.Algorithms.Xenophyte.Centralized.Networking.Solo;

public sealed class NetworkConnection
{
    public required Uri Uri { get; init; }
    
    public required string WalletAddress { get; init; }
    
    public TimeSpan TimeoutDuration { get; init; } = TimeSpan.FromMilliseconds(5000);
}