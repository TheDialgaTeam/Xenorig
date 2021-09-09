using System.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TheDialgaTeam.Core.Logger.Extensions.Logging;
using Xirorig.Options;

namespace Xirorig
{
    internal class ProgramContext
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly IOptionsMonitor<XirorigOptions> _optionsMonitor;

        public CancellationToken ApplicationShutdownCancellationToken => _hostApplicationLifetime.ApplicationStopping;

        public XirorigOptions Options => _optionsMonitor.CurrentValue;

        public ILoggerTemplate<ProgramContext> Logger { get; }

        public ProgramContext(IHostApplicationLifetime hostApplicationLifetime, IOptionsMonitor<XirorigOptions> optionsMonitor, ILoggerTemplate<ProgramContext> logger)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _optionsMonitor = optionsMonitor;
            Logger = logger;
        }
    }
}