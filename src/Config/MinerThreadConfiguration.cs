using System.Threading;

namespace TheDialgaTeam.Xiropht.Xirorig.Config
{
    public class MinerThreadConfiguration
    {
        public ThreadPriority ThreadPriority { get; }

        public bool ShareRange { get; }

        public int MinMiningRangePercentage { get; }

        public int MaxMiningRangePercentage { get; }

        public MinerThreadConfiguration(ThreadPriority threadPriority, bool shareRange, int minMiningRangePercentage, int maxMiningRangePercentage)
        {
            ThreadPriority = threadPriority;
            ShareRange = shareRange;
            MinMiningRangePercentage = minMiningRangePercentage;
            MaxMiningRangePercentage = maxMiningRangePercentage;
        }
    }
}