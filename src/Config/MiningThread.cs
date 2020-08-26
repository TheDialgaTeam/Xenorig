using System.Threading;

namespace TheDialgaTeam.Xiropht.Xirorig.Config
{
    public class MiningThread
    {
        public MiningJob JobType { get; }

        public ThreadPriority ThreadPriority { get; }

        public bool ShareRange { get; }

        public int MinMiningRangePercentage { get; }

        public int MaxMiningRangePercentage { get; }

        public MiningThread(MiningJob jobType, ThreadPriority threadPriority, bool shareRange, int minMiningRangePercentage, int maxMiningRangePercentage)
        {
            JobType = jobType;
            ThreadPriority = threadPriority;
            ShareRange = shareRange;
            MinMiningRangePercentage = minMiningRangePercentage;
            MaxMiningRangePercentage = maxMiningRangePercentage;
        }
    }
}