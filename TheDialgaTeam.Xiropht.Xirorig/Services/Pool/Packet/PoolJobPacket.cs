namespace TheDialgaTeam.Xiropht.Xirorig.Services.Pool.Packet
{
    public sealed class PoolJobPacket
    {
        public const string Block = "block";
        public const string BlockTimestampCreate = "block_timestamp_create";
        public const string BlockKey = "block_key";
        public const string BlockIndication = "block_indication";
        public const string BlockDifficulty = "block_difficulty";
        public const string JobIndication = "job_indication";
        public const string JobDifficulty = "difficulty";
        public const string JobMinRange = "min_range";
        public const string JobMaxRange = "max_range";
        public const string JobMethodName = "method_name";
        public const string JobMethodAesRound = "aes_round";
        public const string JobMethodAesSize = "aes_size";
        public const string JobMethodAesKey = "aes_key";
        public const string JobMethodXorKey = "xor_key";
        public const string JobKeyEncryption = "job_key_encryption";
    }
}