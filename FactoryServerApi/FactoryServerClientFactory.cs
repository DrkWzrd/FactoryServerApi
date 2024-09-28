using FactoryServerApi.Http;
using FactoryServerApi.Udp;
using Microsoft.Extensions.DependencyInjection;

namespace FactoryServerApi;

internal class FactoryServerClientFactory : IFactoryServerClientFactory
{
    private readonly IServiceProvider _sProv;

    public FactoryServerClientFactory(IServiceProvider sProv)
    {
        _sProv = sProv;
    }

    public async Task<IFactoryServerClient> BuildFactoryServerClientAsync(string host, int port, string? apiToken = null, CancellationToken cancellationToken = default)
    {
        var udpFactory = _sProv.GetRequiredService<IFactoryServerUdpClientFactory>();
        var httpFactory = _sProv.GetRequiredService<IFactoryServerHttpClientFactory>();
        var pollClient = await udpFactory.BuildFactoryServerUdpClientAsync(host, port, cancellationToken);
        var pingClient = await udpFactory.BuildFactoryServerUdpClientAsync(host, port, cancellationToken);
        var httpClient = await httpFactory.BuildFactoryServerHttpClientAsync(host, port, apiToken, cancellationToken);
        var client = new FactoryServerClient(pollClient, pingClient, httpClient);

        return client;
    }

}
