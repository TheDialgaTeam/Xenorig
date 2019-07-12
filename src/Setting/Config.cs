using System.Threading;

namespace TheDialgaTeam.Xiropht.Xirorig.Setting
{
    public sealed class Config
    {
        public enum MiningMode
        {
            Solo = 0,
            SoloProxy = 1,
            Pool = 2,
            PoolProxy = 3
        }

        public enum MiningJob
        {
            RandomJob = 0,
            AdditionJob = 1,
            SubtractionJob = 2,
            MultiplicationJob = 3,
            DivisionJob = 4,
            ModulusJob = 5
        }

        public enum MiningPriority
        {
            Shares = 0,
            Normal = 1,
            Block = 2
        }

        public sealed class MiningSolo
        {
            public string WalletAddress { get; set; } = "";
        }

        public sealed class MiningSoloProxy
        {
            public string Host { get; set; } = "";

            public ushort Port { get; set; }

            public string WorkerId { get; set; } = "";
        }

        public sealed class MiningPool
        {
            public string Host { get; set; } = "";

            public ushort Port { get; set; }

            public string WalletAddress { get; set; } = "";

            public string WorkerId { get; set; } = "";
        }

        public sealed class MiningThread
        {
            public MiningJob JobType { get; set; } = MiningJob.RandomJob;

            public ThreadPriority ThreadPriority { get; set; } = ThreadPriority.Normal;

            public MiningPriority MiningPriority { get; set; } = MiningPriority.Normal;

            public bool ShareRange { get; set; } = false;

            public int MinMiningRangePercentage { get; set; } = 0;

            public int MaxMiningRangePercentage { get; set; } = 100;
        }

        public int DonateLevel { get; set; } = 5;

        public int PrintTime { get; set; } = 10;

        public bool Safe { get; set; } = true;

        public MiningMode Mode { get; set; } = MiningMode.Solo;

        public MiningSolo Solo { get; set; } = new MiningSolo();

        public MiningSoloProxy[] SoloProxies { get; set; } = { new MiningSoloProxy() };

        public MiningPool[] Pools { get; set; } = { new MiningPool() };

        public MiningThread[] Threads { get; set; } = { new MiningThread() };
    }
}