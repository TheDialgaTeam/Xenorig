using Xirorig.Network.Api.JobResult;

namespace Xirorig.Algorithm.Xiropht.Decentralized.Api.JobResult.Models
{
    internal record MiningShare(MiningPowShare MiningPowShareObject, long PacketTimestamp) : IJobResult;
}