using Newtonsoft.Json;

namespace Xirorig.Network.Api.Models
{
    internal class MiningShareResponse
    {
        [JsonProperty("MiningPoWShareStatus")]
        public MiningShareResult MiningShareResult { get; set; }

        public long PacketTimestamp { get; set; }
    }
}