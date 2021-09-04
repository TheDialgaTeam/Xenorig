using System;
using System.Threading.Tasks;
using Xirorig.Network.Api.JobResult;
using Xirorig.Network.Api.JobTemplate;
using Xirorig.Options;

namespace Xirorig.Network
{
    internal delegate void NetworkConnectionStatus(Pool pool, Exception? exception);

    internal delegate void NetworkJob(Pool pool, IJobTemplate jobTemplate, string difficulty, long height);

    internal delegate void NetworkJobResult(Pool pool, bool isAccepted, string reason, string difficulty, double ping);

    internal interface INetwork : IDisposable
    {
        event NetworkConnectionStatus? Connected;
        event NetworkConnectionStatus? Disconnected;
        event NetworkJob? NewJob;
        event NetworkJobResult? JobResult;

        void StartNetwork();

        void StopNetwork();

        Task SubmitJobAsync(IJobTemplate jobTemplate, IJobResult jobResult);
    }
}