using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using TheDialgaTeam.Xiropht.Xirorig.Network;

namespace TheDialgaTeam.Xiropht.Xirorig.Miner
{
    public class MinerHostedService : IHostedService
    {
        private readonly XirorigToSeedNetwork _xirorigToSeedNetwork;
        private readonly CpuSoloMiners _cpuSoloMiners;
        private readonly ILogger<MinerHostedService> _logger;

        public long TotalGoodBlocksSubmitted { get; private set; }

        public long TotalBadBlocksSubmitted { get; private set; }

        public MinerHostedService(XirorigToSeedNetwork xirorigToSeedNetwork, CpuSoloMiners cpuSoloMiners, ILogger<MinerHostedService> logger)
        {
            _xirorigToSeedNetwork = xirorigToSeedNetwork;
            _cpuSoloMiners = cpuSoloMiners;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _xirorigToSeedNetwork.Disconnected += XirorigToSeedNetworkOnDisconnected;
            _xirorigToSeedNetwork.LoginResult += XirorigToSeedNetworkOnLoginResult;
            _xirorigToSeedNetwork.NewJob += XirorigToSeedNetworkOnNewJob;
            _xirorigToSeedNetwork.BlockResult += XirorigToSeedNetworkOnBlockResult;

            await _xirorigToSeedNetwork.StartAsync(cancellationToken).ConfigureAwait(false);
            _cpuSoloMiners.StartMinerService();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _xirorigToSeedNetwork.StopAsync().ConfigureAwait(false);

            _xirorigToSeedNetwork.Disconnected -= XirorigToSeedNetworkOnDisconnected;
            _xirorigToSeedNetwork.LoginResult -= XirorigToSeedNetworkOnLoginResult;
            _xirorigToSeedNetwork.NewJob -= XirorigToSeedNetworkOnNewJob;
            _xirorigToSeedNetwork.BlockResult -= XirorigToSeedNetworkOnBlockResult;
        }

        private void XirorigToSeedNetworkOnDisconnected(XirorigToSeedNetwork arg1, string arg2)
        {
            _logger.LogInformation("\u001b[30;1m{timestamp:yyyy-MM-dd HH:mm:ss}\u001b[0m \u001b[31;1m[{host:l}] Disconnected\u001b[0m", DateTimeOffset.Now, arg2);
        }

        private void XirorigToSeedNetworkOnLoginResult(XirorigToSeedNetwork arg1, string arg2, bool arg3)
        {
            _logger.LogInformation(arg3 ? "\u001b[30;1m{timestamp:yyyy-MM-dd HH:mm:ss}\u001b[0m use solo \u001b[36;1m{host:l}\u001b[0m" : "\u001b[30;1m{timestamp:yyyy-MM-dd HH:mm:ss}\u001b[0m \u001b[31;1m[{host}] Login failed. Reason: Invalid wallet address.\u001b[0m", DateTimeOffset.Now, arg2);
        }

        private void XirorigToSeedNetworkOnNewJob(XirorigToSeedNetwork arg1, string arg2, JObject arg3)
        {
            _cpuSoloMiners.UpdateJob(arg3);
            _logger.LogInformation("\u001b[30;1m{timestamp:yyyy-MM-dd HH:mm:ss}\u001b[0m \u001b[35;1mnew job\u001b[0m from {host:l} diff {blockDifficulty} algo {algo:l} height {height}", DateTimeOffset.Now, arg2, arg3["DIFFICULTY"]!.Value<decimal>(), arg3["METHOD"]!.Value<string>(), arg3["ID"]!.Value<long>());
        }

        private void XirorigToSeedNetworkOnBlockResult(XirorigToSeedNetwork arg1, bool arg2, string arg3)
        {
            if (arg2)
            {
                TotalGoodBlocksSubmitted++;
                _logger.LogInformation("\u001b[30;1m{timestamp:yyyy-MM-dd HH:mm:ss}\u001b[0m \u001b[32;1maccepted\u001b[0m ({good}/{bad})", DateTimeOffset.Now, TotalGoodBlocksSubmitted, TotalBadBlocksSubmitted);
            }
            else
            {
                TotalBadBlocksSubmitted++;
                _logger.LogInformation("\u001b[30;1m{timestamp:yyyy-MM-dd HH:mm:ss}\u001b[0m \u001b[31;1mrejected\u001b[0m ({good}/{bad}) - {reason:l}", DateTimeOffset.Now, TotalGoodBlocksSubmitted, TotalBadBlocksSubmitted, arg3);
            }
        }
    }
}