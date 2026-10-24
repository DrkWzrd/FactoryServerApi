using FactoryServerApiNew.Http;
using FactoryServerApiNew.Udp;
using Microsoft.Extensions.DependencyInjection;

namespace FactoryServerApiNew;

public interface IFactoryServerApiClientFactory
{
    /// <summary>
    /// Builds a client. <paramref name="port"/> is the game server port; both the HTTPS API
    /// and the UDP Lightweight Query endpoint listen on that same port (7777 by default).
    /// </summary>
    Task<IFactoryServerApiClient> BuildFactoryServerApiClientAsync(string host, int port, string? apiToken = null, CancellationToken cancellationToken = default);
}

internal class FactoryServerApiClientFactory : IFactoryServerApiClientFactory
{
    private readonly IServiceProvider _sProv;

    public FactoryServerApiClientFactory(IServiceProvider sProv)
    {
        _sProv = sProv;
    }

    public async Task<IFactoryServerApiClient> BuildFactoryServerApiClientAsync(string host, int port, string? apiToken = null, CancellationToken cancellationToken = default)
    {
        IFactoryServerUdpClientFactory udpFactory = _sProv.GetRequiredService<IFactoryServerUdpClientFactory>();
        IFactoryServerHttpClientFactory httpFactory = _sProv.GetRequiredService<IFactoryServerHttpClientFactory>();
        IFactoryServerUdpClient udp = await udpFactory.BuildFactoryServerUdpClientAsync(host, port, cancellationToken);
        IFactoryServerHttpClient http = await httpFactory.BuildFactoryServerHttpClientAsync(host, port, apiToken, cancellationToken);

        return new FactoryServerApiClient(http, udp);
    }
}
