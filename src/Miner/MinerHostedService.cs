using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xiropht_Connector_All.Setting;

namespace TheDialgaTeam.Xiropht.Xirorig.Miner
{
    public class MinerHostedService : IHostedService
    {
        private readonly ILogger<MinerHostedService> _logger;

        private string[] _seedNodeIps;

        public long TotalGoodSharesSubmitted { get; private set; }

        public long TotalBadSharesSubmitted { get; private set; }

        public MinerHostedService(ILogger<MinerHostedService> logger)
        {
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await BuildSeedNodeIpAsync().ConfigureAwait(false);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task BuildSeedNodeIpAsync()
        {
            var listOfSeeds = new Dictionary<string, long>();

            foreach (var (seedNodeIp, _) in ClassConnectorSetting.SeedNodeIp)
            {
                using var ping = new Ping();

                try
                {
                    var result = await ping.SendPingAsync(IPAddress.Parse(seedNodeIp), 5000).ConfigureAwait(false);

                    if (result.Status == IPStatus.Success)
                    {
                        listOfSeeds.Add(seedNodeIp, result.RoundtripTime);
                    }
                }
                catch (PingException)
                {
                    listOfSeeds.Add(seedNodeIp, long.MaxValue);
                }
            }

            var seedsToLoad = listOfSeeds.OrderBy(a => a.Value);
            var seedNodeIps = new List<string>();
            var index = 0;

            foreach (var (seedNodeIp, pingValue) in seedsToLoad)
            {
                seedNodeIps.Add(seedNodeIp);
                _logger.LogInformation(" \u001b[32;1m*\u001b[0m SOLO #{index,-7:l}\u001b[36;1m{nodeIp:l}:{nodePort}\u001b[0m ({nodePing}ms)", (++index).ToString(), seedNodeIp, ClassConnectorSetting.SeedNodePort, pingValue);
            }

            _seedNodeIps = seedNodeIps.ToArray();
        }
    }
}