namespace FactoryServerApi;

public interface IFactoryServerClientFactory
{
    Task<IFactoryServerClient> BuildFactoryServerClientAsync(string host, int port, string? apiToken = null, CancellationToken cancellationToken = default);
}