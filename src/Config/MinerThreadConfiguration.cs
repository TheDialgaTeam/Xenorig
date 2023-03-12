using System.Threading;
using Microsoft.Extensions.Configuration;

namespace TheDialgaTeam.Xiropht.Xirorig.Config
{
    public readonly struct MinerThreadConfiguration
    {
        public ThreadPriority ThreadPriority { get; }

        public bool EasyBlockOnly { get; }

        public MinerThreadConfiguration(ThreadPriority threadPriority, bool easyBlockOnly)
        {
            ThreadPriority = threadPriority;
            EasyBlockOnly = easyBlockOnly;
        }

        public MinerThreadConfiguration(IConfiguration minerThreadConfigurationSection, in MinerThreadConfiguration defaultMinerThreadConfiguration)
        {
            ThreadPriority = minerThreadConfigurationSection.GetValue("ThreadPriority", defaultMinerThreadConfiguration.ThreadPriority);
            EasyBlockOnly = minerThreadConfigurationSection.GetValue("EasyBlockOnly", defaultMinerThreadConfiguration.EasyBlockOnly);
        }
    }
}