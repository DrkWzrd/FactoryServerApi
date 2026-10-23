using System.Net.Http.Headers;
using System.Net.Mime;
using System.Reflection;
using FactoryServerApi.Http;
using FactoryServerApi.Udp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FactoryServerApi;

public static class IServiceCollectionExtensions
{
    private const string DefaultSettingsFilename = $"{nameof(FactoryServerApi)}.settings.json";

    private static readonly HttpClientHandler NoValidationHandler = new()
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };

    private static readonly HttpClientHandler DefaultHandler = new();

    public static IHostApplicationBuilder AddFactoryServerServices(this IHostApplicationBuilder host)
    {
        // An optional settings file in the current directory can override the in-code defaults.
        host.Configuration.AddJsonFile(DefaultSettingsFilename, false);

        // Defaults live in the option classes; a settings file only overrides the keys present.
        host.Services.AddOptions<HttpOptions>().BindConfiguration("httpConfiguration");
        host.Services.AddOptions<SslOptions>().BindConfiguration("sslConfiguration");
        host.Services.AddOptions<UdpOptions>().BindConfiguration("udpConfiguration");

        host.Services.AddHttpClient("factoryServerHttpClient", (sProv, hClient) =>
            {
                IOptions<HttpOptions> options = sProv.GetRequiredService<IOptions<HttpOptions>>();
                hClient.Timeout = options.Value.ConnectionTimeout;

                hClient.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(MediaTypeNames.Application.Json));
                hClient.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(MediaTypeNames.Application.Octet));

                ProductInfoHeaderValue pihv = new(options.Value.UserAgentAppName,
                    options.Value.UserAgentAppVersion?.ToString()
                        ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                        ?? "0.0");

                hClient.DefaultRequestHeaders.UserAgent.Add(pihv);
            })
            .ConfigurePrimaryHttpMessageHandler(sProv =>
            {
                IOptions<SslOptions> options = sProv.GetRequiredService<IOptions<SslOptions>>();

                switch (options?.Value.ServerCertificateValidationStrategy ?? null)
                {
                    case SslValidationStrategy.NoValidation:
                        return NoValidationHandler;
                    case SslValidationStrategy.Custom:
                        IServerCertificateValidationStrategy certValidationStrategy = sProv.GetRequiredService<IServerCertificateValidationStrategy>();
                        return new HttpClientHandler()
                        {
                            ServerCertificateCustomValidationCallback = certValidationStrategy.CustomValidationCallback,
                        };
                    default:
                        return DefaultHandler;
                }
            });

        host.Services.AddKeyedSingleton("factoryServerLocalSystemTimeProvider", TimeProvider.System)
            .AddSingleton<IFactoryServerUdpClientFactory, FactoryServerUdpClientFactory>()
            .AddSingleton<IFactoryServerHttpClientFactory, FactoryServerHttpClientFactory>()
            .AddSingleton<IFactoryServerClientFactory, FactoryServerClientFactory>();

        return host;
    }

}