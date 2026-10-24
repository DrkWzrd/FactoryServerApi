using FactoryServerApiNew.Http;
using FactoryServerApiNew.Udp;
using Microsoft.Extensions.DependencyInjection;

namespace FactoryServerApiNew;

public interface IFactoryServerClientFactory
{
    /// <summary>
    /// Builds a client. <paramref name="port"/> is the game server port; both the HTTPS API
    /// and the UDP Lightweight Query endpoint listen on that same port (7777 by default).
    /// An optional API token (issued by the <c>server.GenerateAPIToken</c> console command)
    /// can be passed to authenticate as a third party application without logging in.
    /// </summary>
    Task<IFactoryServerClient> BuildFactoryServerClientAsync(string host, int port, string? apiToken = null, CancellationToken cancellationToken = default);
}

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
        IFactoryServerUdpClient udp = await udpFactory.BuildFactoryServerUdpClientAsync(host, port, cancellationToken);
        IFactoryServerHttpClient http = await httpFactory.BuildFactoryServerHttpClientAsync(host, port, apiToken, cancellationToken);

        return new FactoryServerClient(http, udp);
    }
}
