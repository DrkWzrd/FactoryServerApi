using FactoryServerApiNew.Http;
using FactoryServerApiNew.Udp;

namespace FactoryServerApiNew;

/// <summary>
/// Lean access point to both server transports: the HTTPS API and the UDP Lightweight Query,
/// which listen on the same server port (7777 by default).
/// </summary>
public interface IFactoryServerApiClient : IDisposable
{
    /// <summary>
    /// The HTTPS API access point.
    /// </summary>
    IFactoryServerHttpClient Http { get; }

    /// <summary>
    /// The UDP Lightweight Query access point.
    /// </summary>
    IFactoryServerUdpClient Udp { get; }

    Task SetAuthenticationTokenAsync(string authToken, CancellationToken cancellationToken = default);
    Task SetPlayerIdAsync(FactoryGamePlayerId playerId, CancellationToken cancellationToken = default);
}

internal class FactoryServerApiClient : IFactoryServerApiClient
{
    public IFactoryServerHttpClient Http { get; }
    public IFactoryServerUdpClient Udp { get; }

    internal FactoryServerApiClient(IFactoryServerHttpClient httpClient, IFactoryServerUdpClient udpClient)
    {
        Http = httpClient;
        Udp = udpClient;
    }

    public Task SetAuthenticationTokenAsync(string authToken, CancellationToken cancellationToken = default)
        => Http.SetAuthenticationTokenAsync(authToken, cancellationToken);

    public Task SetPlayerIdAsync(FactoryGamePlayerId playerId, CancellationToken cancellationToken = default)
        => Http.SetPlayerIdAsync(playerId, cancellationToken);

    public void Dispose() => Udp.Dispose();
}
