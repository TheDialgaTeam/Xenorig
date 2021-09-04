using System.Text.Json.Serialization;

namespace Xirorig.Algorithm.Xiropht.Decentralized.Api.JobResult
{
    internal class SendMiningShare
    {
        internal record Request([property: JsonPropertyOrder(1)] string PacketContentObjectSerialized)
        {
            [JsonInclude]
            [JsonPropertyOrder(0)]
            public int PacketType => 10;
        }

        internal record Response(string PacketObjectSerialized);
    }
}