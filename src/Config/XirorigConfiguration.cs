using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Xenophyte_Connector_All.Setting;

namespace TheDialgaTeam.Xiropht.Xirorig.Config
{
    public class XirorigConfiguration
    {
        public int PrintTime { get; }

        public string? WalletAddress { get; }

        public string[] SeedNodeIpAddresses { get; }

        public MinerThreadConfiguration[] MinerThreadConfigurations { get; }

        public XirorigConfiguration(IConfiguration configuration)
        {
            PrintTime = configuration.GetValue("Xirorig:PrintTime", 10);
            WalletAddress = configuration.GetValue<string?>("Xirorig:WalletAddress");

            var seedNodeIpAddressesSection = configuration.GetSection("Xirorig:SeedNodeIpAddresses").GetChildren();
            var seedNodeIpAddresses = new List<string>();

            foreach (var seedNoteIdAddressSection in seedNodeIpAddressesSection)
            {
                seedNodeIpAddresses.Add(seedNoteIdAddressSection.Value);
            }

            if (seedNodeIpAddresses.Count == 0)
            {
                var defaultSeedNodeIpAddresses = ClassConnectorSetting.SeedNodeIp.Keys;

                foreach (var defaultSeedNodeIpAddress in defaultSeedNodeIpAddresses)
                {
                    seedNodeIpAddresses.Add(defaultSeedNodeIpAddress);
                }
            }

            SeedNodeIpAddresses = seedNodeIpAddresses.ToArray();

            var defaultNumberOfThreads = configuration.GetValue("Xirorig:NumberOfThreads", 1);
            var defaultThreadPriority = configuration.GetValue("Xirorig:ThreadPriority", ThreadPriority.Normal);
            var defaultMinMiningRangePercentage = configuration.GetValue("Xirorig:MinMiningRangePercentage", 0);
            var defaultMaxMiningRangePercentage = configuration.GetValue("Xirorig:MaxMiningRangePercentage", 100);
            var defaultEasyBlockOnly = configuration.GetValue("Xirorig:EasyBlockOnly", false);
            var defaultMinerThreadConfiguration = new MinerThreadConfiguration(defaultThreadPriority, defaultMinMiningRangePercentage, defaultMaxMiningRangePercentage, defaultEasyBlockOnly);

            var minerThreadConfigurationsSection = configuration.GetSection("Xirorig:Threads").GetChildren();
            var minerThreadConfigurations = new List<MinerThreadConfiguration>();

            foreach (var minerThreadConfigurationSection in minerThreadConfigurationsSection)
            {
                minerThreadConfigurations.Add(new MinerThreadConfiguration(minerThreadConfigurationSection, defaultMinerThreadConfiguration));
            }

            if (minerThreadConfigurations.Count == 0)
            {
                for (var i = defaultNumberOfThreads - 1; i >= 0; i--)
                {
                    minerThreadConfigurations.Add(defaultMinerThreadConfiguration);
                }
            }

            MinerThreadConfigurations = minerThreadConfigurations.ToArray();
        }
    }
}