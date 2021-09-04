using System.Text.Json.Serialization;

namespace Xirorig.Algorithm.Xiropht.Decentralized.Api.JobResult
{
    internal class SendMiningShare
    {
        internal record Request(string PacketContentObjectSerialized)
        {
            [JsonInclude]
            public int PacketType => 10;
        }

        internal record Response(string PacketObjectSerialized);
    }
}