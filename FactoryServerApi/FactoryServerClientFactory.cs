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
        IFactoryServerUdpClientFactory udpFactory = _sProv.GetRequiredService<IFactoryServerUdpClientFactory>();
        IFactoryServerHttpClientFactory httpFactory = _sProv.GetRequiredService<IFactoryServerHttpClientFactory>();
        IFactoryServerUdpClient pollClient = await udpFactory.BuildFactoryServerUdpClientAsync(host, port, cancellationToken);
        IFactoryServerUdpClient pingClient = await udpFactory.BuildFactoryServerUdpClientAsync(host, port, cancellationToken);
        IFactoryServerHttpClient httpClient = await httpFactory.BuildFactoryServerHttpClientAsync(host, port, apiToken, cancellationToken);
        FactoryServerClient client = new(pollClient, pingClient, httpClient);

        return client;
    }

}
