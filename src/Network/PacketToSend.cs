namespace TheDialgaTeam.Xiropht.Xirorig.Network
{
    public readonly struct PacketToSend
    {
        public string Packet { get; }

        public bool IsEncrypted { get; }

        public PacketToSend(string packet, bool isEncrypted)
        {
            Packet = packet;
            IsEncrypted = isEncrypted;
        }
    }
}