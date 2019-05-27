using TheDialgaTeam.Xiropht.Xirorig.Services.Pool;

namespace TheDialgaTeam.Xiropht.Xirorig.Services.Setting
{
    public sealed class Config
    {
        public sealed class MiningThread
        {
            public PoolMiner.JobType JobType { get; set; } = PoolMiner.JobType.RandomJob;

            public int ThreadPriority { get; set; } = 2;

            public int ThreadAffinityToCpu { get; set; }

            public bool PrioritizePoolSharesVsBlock { get; set; } = false;
        }

        public string Host { get; set; } = "";

        public ushort Port { get; set; } = 0;

        public string WalletAddress { get; set; } = "";

        public MiningThread[] MiningThreads { get; set; }
    }
}