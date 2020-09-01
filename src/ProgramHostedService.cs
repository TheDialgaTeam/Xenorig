using System;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TheDialgaTeam.Xiropht.Xirorig.Config;

namespace TheDialgaTeam.Xiropht.Xirorig
{
    public class ProgramHostedService : IHostedService
    {
        private readonly ILogger<ProgramHostedService> _logger;
        private readonly XirorigConfiguration _xirorigConfiguration;

        public ProgramHostedService(ILogger<ProgramHostedService> logger, XirorigConfiguration xirorigConfiguration)
        {
            _logger = logger;
            _xirorigConfiguration = xirorigConfiguration;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var frameworkVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;

            Console.Title = $"Xirorig v{version} ({frameworkVersion})";

            _logger.LogInformation(" \u001b[32;1m*\u001b[0m ABOUT        \u001b[36;1mXirorig/{version:l}\u001b[0m {frameworkVersion:l}", version, frameworkVersion);
            _logger.LogInformation(" \u001b[32;1m*\u001b[0m THREADS      \u001b[36;1m{numThreads}\u001b[0m", _xirorigConfiguration.MinerThreadConfigurations.Length);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}