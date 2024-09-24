using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FactoryServerApi.Http;

internal class FactoryServerHttpClientFactory : IFactoryServerHttpClientFactory
{

    private readonly IServiceProvider _sProv;

    public FactoryServerHttpClientFactory(IServiceProvider sProv)
    {
        _sProv = sProv;
    }

    public async Task<IFactoryServerHttpClient> BuildFactoryServerHttpClientAsync(string host, int port, string? authToken = null, CancellationToken cancellationToken = default)
    {
        var httpClientFactory = _sProv.GetRequiredService<IHttpClientFactory>();
        var options = _sProv.GetRequiredService<IOptions<HttpOptions>>();

        var client = new FactoryServerHttpClient(httpClientFactory, host, port, options);

        if (authToken is not null)
            await client.SetAuthenticationTokenAsync(authToken, cancellationToken);

        return client;
    }

}
