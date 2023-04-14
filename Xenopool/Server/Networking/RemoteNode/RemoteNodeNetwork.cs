namespace Xenopool.Server.Networking.RemoteNode;

public sealed class RemoteNodeNetwork : IDisposable
{
    private readonly Options.RemoteNode _remoteNode;
    private readonly HttpClient _httpClient;
    
    public RemoteNodeNetwork(Options.RemoteNode remoteNode)
    {
        _remoteNode = remoteNode;
        _httpClient = new HttpClient
        {
            BaseAddress = new UriBuilder { Host = remoteNode.Host, Port = remoteNode.Port }.Uri,
            Timeout = TimeSpan.FromSeconds(remoteNode.NetworkTimeoutDuration)
        };
    }
    
    public void Dispose()
    {
        _httpClient.Dispose();
    }
}