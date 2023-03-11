using System.Threading;
using System.Threading.Tasks;

namespace Xenorig.Algorithms;

public interface IAlgorithm
{
    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);

    void PrintHashrate();

    void PrintStats();

    void PrintCurrentJob();
}