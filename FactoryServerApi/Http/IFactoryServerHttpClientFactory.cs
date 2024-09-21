namespace FactoryServerApi.Http;

public interface IFactoryServerHttpClientFactory
{
    Task<IFactoryServerHttpClient> BuildFactoryServerHttpClientAsync(string host, int port, AuthenticationData? authData = null, CancellationToken cancellationToken = default);
}
