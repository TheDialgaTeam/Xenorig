using Xirorig.Miner.Network.Api.JobResult;

namespace Xirorig.Algorithms.Xiropht.Decentralized.Api.JobResult.Models
{
    internal record MiningShare(MiningPowShare MiningPowShareObject, long PacketTimestamp) : IJobResult;
}