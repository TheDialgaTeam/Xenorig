using System.Threading;
using Newtonsoft.Json;
using TheDialgaTeam.Xiropht.Xirorig.Services.Pool;

namespace TheDialgaTeam.Xiropht.Xirorig.Services.Setting
{
    public sealed class Config
    {
        public sealed class MiningPool
        {
            public string Host { get; set; } = "pool.xir.aggressivegaming.org:4444";

            public string WalletAddress { get; set; } = "";

            public string WorkerId { get; set; } = "";

            [JsonProperty("url")]
            private string HostAlias
            {
                set => Host = value;
            }

            [JsonProperty("user")]
            private string WalletAddressAlias
            {
                set => WalletAddress = value;
            }

            [JsonProperty("pass")]
            private string WorkerIdAlias
            {
                set => WorkerId = value;
            }
        }

        public sealed class MiningThread
        {
            public PoolMiner.JobType JobType { get; set; } = PoolMiner.JobType.RandomJob;

            public ThreadPriority ThreadPriority { get; set; } = ThreadPriority.Normal;

            public int ThreadAffinityToCpu { get; set; }

            public bool PrioritizePoolSharesVsBlock { get; set; } = false;

            [JsonProperty("affine_to_cpu")]
            private int ThreadAffinityToCpuAlias
            {
                set => ThreadAffinityToCpu = value;
            }
        }

        public int DonateLevel { get; set; } = 5;

        public int PrintTime { get; set; } = 10;

        public bool Safe { get; set; } = true;

        public MiningPool[] Pools { get; set; } = { new MiningPool() };

        public MiningThread[] Threads { get; set; }

        [JsonProperty]
        private string Host
        {
            set => Pools[0].Host = value;
        }

        [JsonProperty]
        private ushort Port
        {
            set
            {
                if (Pools[0].Host.Contains(":"))
                    Pools[0].Host = Pools[0].Host.Remove(Pools[0].Host.IndexOf(':')) + ":" + value;
                else
                    Pools[0].Host = $"{Pools[0].Host}:{value}";
            }
        }

        [JsonProperty]
        private string WalletAddress
        {
            set => Pools[0].WalletAddress = value;
        }

        [JsonProperty("donate-level")]
        private int DonateLevelAlias
        {
            set => DonateLevel = value;
        }

        [JsonProperty("pools")]
        private MiningPool[] PoolsAlias
        {
            set => Pools = value;
        }

        [JsonProperty("print-time")]
        private int PrintTimeAlias
        {
            set => PrintTime = value;
        }

        [JsonProperty("threads")]
        private MiningThread[] ThreadsAlias
        {
            set => Threads = value;
        }

        [JsonProperty("MiningThreads")]
        private MiningThread[] ThreadsAlias2
        {
            set => Threads = value;
        }
    }
}