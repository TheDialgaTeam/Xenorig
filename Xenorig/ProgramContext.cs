using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xenorig.Options;

namespace Xenorig;

internal class ProgramContext : IDisposable
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IDisposable? _optionsDisposable;

    public CancellationToken ApplicationShutdownCancellationToken => _hostApplicationLifetime.ApplicationStopping;

    public XenorigOptions Options { get; private set; }

    public ProgramContext(IHostApplicationLifetime hostApplicationLifetime, IOptionsMonitor<XenorigOptions> optionsMonitor)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _optionsDisposable = optionsMonitor.OnChange(XenorigOptionsChange);

        Options = optionsMonitor.CurrentValue;
    }

    private void XenorigOptionsChange(XenorigOptions options)
    {
        Options = options;
    }

    public void Dispose()
    {
        _optionsDisposable?.Dispose();
    }
}