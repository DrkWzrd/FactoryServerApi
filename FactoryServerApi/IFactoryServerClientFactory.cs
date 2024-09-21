namespace FactoryServerApi;

public interface IFactoryServerClientFactory
{
    Task<IFactoryServerClient> BuildFactoryServerClientAsync(string host, int port, AuthenticationData? authData = null, CancellationToken cancellationToken = default);
}