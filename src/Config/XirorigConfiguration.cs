using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Configuration;

namespace TheDialgaTeam.Xiropht.Xirorig.Config
{
    public class XirorigConfiguration
    {
        public int PrintTime { get; }

        public bool Safe { get; }

        public string WalletAddress { get; }

        public string[] SeedNodeIpAddresses { get; }

        public int SeedNodePort { get; }

        public int SeedNodeTokenPort { get; }

        public MiningThread[] Threads { get; }

        public XirorigConfiguration(IConfiguration configuration)
        {
            PrintTime = configuration.GetValue<int>("Xirorig:PrintTime");
            Safe = configuration.GetValue<bool>("Xirorig:Safe");
            WalletAddress = configuration.GetValue<string>("Xirorig:WalletAddress");

            var seedNodeIpAddressesConfig = configuration.GetSection("Xirorig:SeedNodeIpAddresses").GetChildren();
            var seedNodeIpAddresses = new List<string>();

            foreach (var configurationSection in seedNodeIpAddressesConfig)
            {
                seedNodeIpAddresses.Add(configurationSection.Value);
            }

            SeedNodeIpAddresses = seedNodeIpAddresses.ToArray();

            SeedNodePort = configuration.GetValue<int>("Xirorig:SeedNodePort");
            SeedNodeTokenPort = configuration.GetValue<int>("Xirorig:SeedNodeTokenPort");

            var threadsConfig = configuration.GetSection("Xirorig:Threads").GetChildren();
            var miningThreads = new List<MiningThread>();

            foreach (var configurationSection in threadsConfig)
            {
                miningThreads.Add(new MiningThread(
                    configurationSection.GetValue<MiningJob>("JobType"),
                    configurationSection.GetValue<ThreadPriority>("ThreadPriority"),
                    configurationSection.GetValue<bool>("ShareRange"),
                    configurationSection.GetValue<int>("MinMiningRangePercentage"),
                    configurationSection.GetValue<int>("MaxMiningRangePercentage"))
                );
            }

            Threads = miningThreads.ToArray();
        }
    }
}