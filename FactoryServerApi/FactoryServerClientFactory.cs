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

    public async Task<IFactoryServerClient> BuildFactoryServerClientAsync(string host, int port, AuthenticationData? authData = null, CancellationToken cancellationToken = default)
    {
        var udpFactory = _sProv.GetRequiredService<IFactoryServerUdpClientFactory>();
        var httpFactory = _sProv.GetRequiredService<IFactoryServerHttpClientFactory>();
        var udpClient = await udpFactory.BuildFactoryServerUdpClientAsync(host, port, cancellationToken);
        var httpClient = await httpFactory.BuildFactoryServerHttpClientAsync(host, port, authData, cancellationToken);
        return new FactoryServerClient(udpClient, httpClient);
    }

}
