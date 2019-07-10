namespace TheDialgaTeam.Xiropht.Xirorig.Mining.Pool.Packet
{
    public sealed class PoolSharePacket
    {
        public const string Result = "result";

        public const string ResultShareOk = "ok";
        public const string ResultShareInvalid = "invalid share";
        public const string ResultShareDuplicate = "duplicate share";
        public const string ResultShareLowDifficulty = "low difficulty share";
    }
}