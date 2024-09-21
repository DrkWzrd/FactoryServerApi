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


    //client.GetIsAuthenticated() -> true/false-FactoryServerError;
    //client.SetAuthenticationToken(token);
    //client.AdminLogin(pass)
    //client.ClientLogin(pass?)
    public async Task<IFactoryServerHttpClient> BuildFactoryServerHttpClientAsync(string host, int port, AuthenticationData? authData = null, CancellationToken cancellationToken = default)
    {
        var httpClientFactory = _sProv.GetRequiredService<IHttpClientFactory>();
        var options = _sProv.GetRequiredService<IOptions<HttpOptions>>();
        FactoryServerHttpClient client = new FactoryServerHttpClient(httpClientFactory, host, port, options);
        await client.CheckIfServerIsClaimed(cancellationToken);
        if (authData is not null)
        {
            var verificationError = await client.SetAuthenticationDataAndVerifyAsync(authData, cancellationToken);
            if (verificationError is not null)
            {
                await client.SetAuthenticationDataAndVerifyAsync(AuthenticationData.Empty, cancellationToken);
            }
        }

        if(client.AuthenticationData == AuthenticationData.Empty)
        {
            await client.AdministratorLoginAsync("drkwzrdSatisfactoryServer".AsMemory(), cancellationToken);
        }

        if(client.AuthenticationData == AuthenticationData.Empty)
        {
            await client.ClientLoginAsync(null, cancellationToken);
        }

        return client;
    }

}
