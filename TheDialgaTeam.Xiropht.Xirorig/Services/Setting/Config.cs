using System.Threading;
using Newtonsoft.Json;

namespace TheDialgaTeam.Xiropht.Xirorig.Services.Setting
{
    public sealed class Config
    {
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

        public sealed class MiningPool
        {
            public string Host { get; set; } = "";

            public ushort Port { get; set; }

            public string WalletAddress { get; set; } = "";

            public string WorkerId { get; set; } = "";

            [JsonProperty("url")]
            private string HostPortAlias
            {
                set
                {
                    Host = value.Remove(value.IndexOf(':'));
                    Port = ushort.Parse(value.Substring(value.IndexOf(':') + 1));
                }
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
            public MiningJob JobType { get; set; } = MiningJob.RandomJob;

            public ThreadPriority ThreadPriority { get; set; } = ThreadPriority.Normal;

            public int ThreadAffinityToCpu { get; set; }

            public MiningPriority MiningPriority { get; set; } = MiningPriority.Normal;

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
            set => Pools[0].Port = value;
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

        [JsonProperty("print-time")]
        private int PrintTimeAlias
        {
            set => PrintTime = value;
        }

        [JsonProperty("safe")]
        private bool SafeAlias
        {
            set => Safe = value;
        }

        [JsonProperty("pools")]
        private MiningPool[] PoolsAlias
        {
            set => Pools = value;
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