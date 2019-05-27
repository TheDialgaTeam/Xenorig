namespace TheDialgaTeam.Xiropht.Xirorig.Services.Pool.Packet
{
    public sealed class PoolPacket
    {
        public const string Type = "type";
    }

    public sealed class PoolPacketType
    {
        public const string Login = "login";
        public const string KeepAlive = "keep-alive";
        public const string Job = "job";
        public const string Share = "share";
        public const string Submit = "submit";
    }
}