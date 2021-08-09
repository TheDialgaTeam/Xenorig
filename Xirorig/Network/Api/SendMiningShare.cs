namespace Xirorig.Network.Api
{
    internal class SendMiningShare
    {
        internal class Request
        {
            public int PacketType { get; set; } = 10;

            public string PacketContentObjectSerialized { get; set; } = string.Empty;
        }

        internal class Response
        {
            public string PacketObjectSerialized { get; set; } = string.Empty;
        }
    }
}