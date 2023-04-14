namespace Xenorig.Algorithms;

public interface IAlgorithm
{
    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);

    void PrintHashrate();

    void PrintStats();

    void PrintCurrentJob();
}