namespace FactoryServerApi;

public interface IFactoryServerClientFactory
{
    /// <summary>
    /// Builds a client. <paramref name="port"/> is the game server port; both the HTTPS API
    /// and the UDP Lightweight Query endpoint listen on that same port (7777 by default).
    /// </summary>
    Task<IFactoryServerClient> BuildFactoryServerClientAsync(string host, int port, string? apiToken = null, CancellationToken cancellationToken = default);
}