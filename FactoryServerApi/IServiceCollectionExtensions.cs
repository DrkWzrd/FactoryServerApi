using FactoryServerApi.Http;
using FactoryServerApi.Udp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Reflection;

namespace FactoryServerApi;

public static class IServiceCollectionExtensions
{

    private static readonly Func<HttpClientHandler> NoValidationHandlerFunc = () =>
    {
        return new HttpClientHandler()
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
        };
    };

    public static IHostApplicationBuilder AddFactoryServerServices(this IHostApplicationBuilder host)
    {
        host.Configuration.AddJsonFile("FactoryServerApi.settings.json", false);
        host.Services.AddOptions<HttpOptions>().BindConfiguration("httpConfiguration");
        host.Services.AddOptions<SslOptions>().BindConfiguration("sslConfiguration");
        host.Services.AddHttpClient("factoryServerHttpClient", (sProv, hClient) =>
            {
                var options = sProv.GetRequiredService<IOptions<HttpOptions>>();
                hClient.Timeout = options.Value.ConnectionTimeout;
                hClient.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("*/*"));
                var pihv = new ProductInfoHeaderValue(options.Value.UserAgentAppName,
                    options.Value.UserAgentAppVersion?.ToString()
                        ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                        ?? "0.0");
                hClient.DefaultRequestHeaders.UserAgent.Add(pihv);
            })
            .ConfigurePrimaryHttpMessageHandler(sProv =>
            {
                var options = sProv.GetRequiredService<IOptions<SslOptions>>();

                switch (options?.Value.ServerCertificateValidationStrategy ?? null)
                {
                    case SslValidationStrategy.NoValidation:
                        return NoValidationHandlerFunc();
                    case SslValidationStrategy.Custom:
                        var certValidationStrategy = sProv.GetRequiredService<IServerCertificateValidationStrategy>();
                        return new HttpClientHandler()
                        {
                            ServerCertificateCustomValidationCallback = certValidationStrategy.CustomValidationCallback,
                        };
                    default:
                        return new HttpClientHandler();
                }
            });
        host.Services.AddKeyedSingleton("factoryServerLocalSystemTimeProvider", TimeProvider.System)
            .AddSingleton<IFactoryServerUdpClientFactory, FactoryServerUdpClientFactory>()
            .AddTransient<IFactoryServerHttpService, FactoryServerHttpService>();
        return host;
    }

}
