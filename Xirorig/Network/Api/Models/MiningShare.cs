namespace Xirorig.Network.Api.Models
{
    internal class MiningShare
    {
        public MiningPowShare MiningPowShareObject { get; set; } = new();

        public long PacketTimestamp { get; set; }
    }
}