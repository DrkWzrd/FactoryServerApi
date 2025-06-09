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
        IHttpClientFactory httpClientFactory = _sProv.GetRequiredService<IHttpClientFactory>();
        IOptions<HttpOptions> options = _sProv.GetRequiredService<IOptions<HttpOptions>>();

        FactoryServerHttpClient client = new(httpClientFactory, host, port, options);

        if (authToken is not null)
            await client.SetAuthenticationTokenAsync(authToken, cancellationToken);

        return client;
    }

}
