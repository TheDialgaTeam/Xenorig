using System.Text.Json.Serialization;

namespace Xirorig.Algorithms.Xiropht.Decentralized.Api.JobResult.Models
{
    internal class MiningShareResponse
    {
        [JsonPropertyName("MiningPoWShareStatus")]
        public MiningShareResult MiningShareResult { get; set; }

        public long PacketTimestamp { get; set; }
    }
}