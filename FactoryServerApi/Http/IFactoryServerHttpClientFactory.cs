namespace FactoryServerApi.Http;

public interface IFactoryServerHttpClientFactory
{
    Task<IFactoryServerHttpClient> BuildFactoryServerHttpClientAsync(string host, int port, string? authToken = null, CancellationToken cancellationToken = default);
}
