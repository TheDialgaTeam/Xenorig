using System.Threading;
using Microsoft.Extensions.Configuration;

namespace TheDialgaTeam.Xiropht.Xirorig.Config
{
    public readonly struct MinerThreadConfiguration
    {
        public ThreadPriority ThreadPriority { get; }

        public int MinMiningRangePercentage { get; }

        public int MaxMiningRangePercentage { get; }

        public bool EasyBlockOnly { get; }

        public MinerThreadConfiguration(ThreadPriority threadPriority, int minMiningRangePercentage, int maxMiningRangePercentage, bool easyBlockOnly)
        {
            ThreadPriority = threadPriority;
            MinMiningRangePercentage = minMiningRangePercentage;
            MaxMiningRangePercentage = maxMiningRangePercentage;
            EasyBlockOnly = easyBlockOnly;
        }

        public MinerThreadConfiguration(IConfiguration minerThreadConfigurationSection, in MinerThreadConfiguration defaultMinerThreadConfiguration)
        {
            ThreadPriority = minerThreadConfigurationSection.GetValue("ThreadPriority", defaultMinerThreadConfiguration.ThreadPriority);
            MinMiningRangePercentage = minerThreadConfigurationSection.GetValue("MinMiningRangePercentage", defaultMinerThreadConfiguration.MinMiningRangePercentage);
            MaxMiningRangePercentage = minerThreadConfigurationSection.GetValue("MaxMiningRangePercentage", defaultMinerThreadConfiguration.MaxMiningRangePercentage);
            EasyBlockOnly = minerThreadConfigurationSection.GetValue("EasyBlockOnly", defaultMinerThreadConfiguration.EasyBlockOnly);
        }
    }
}