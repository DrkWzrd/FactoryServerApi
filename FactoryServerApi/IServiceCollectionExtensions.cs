using FactoryServerApi.Http;
using FactoryServerApi.Udp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Reflection;

namespace FactoryServerApi;

public static class IServiceCollectionExtensions
{
    private const string _defaultSettingsFilename = $"{nameof(FactoryServerApi)}.settings.json";
    private const string _defaultJsonFileContent =
        """
        {
            "udpConfiguration": {
                "delayBetweenPolls": "00:00:10",
                "messagesPerPoll": 2,
                "timeoutRetriesBeforeStop": 3,
                "protocolMagic": 63189,
                "protocolVersion": 1,
                "messageTermination": 1
            },
            "httpConfiguration": {
                "connectionTimeOut": "00:00:30",
                "userAgentAppName": "factoryServerApi",
                "userAgentAppVersion": "0.3",
                "apiPath": "api/v1/"
            },
            "sslConfiguration": {
                "serverCertificateValidationStrategy": "NoValidation"
            }
        }
        """;

    private static readonly HttpClientHandler NoValidationHandlerFunc = new()
    {
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
    };

    public static IHostApplicationBuilder AddFactoryServerServices(this IHostApplicationBuilder host)
    {
        if (!File.Exists(_defaultSettingsFilename))
            File.WriteAllText(_defaultSettingsFilename, _defaultJsonFileContent);

        host.Configuration.AddJsonFile(_defaultSettingsFilename, false);
        host.Services.AddOptions<HttpOptions>().BindConfiguration("httpConfiguration");
        host.Services.AddOptions<SslOptions>().BindConfiguration("sslConfiguration");
        host.Services.AddOptions<UdpOptions>().BindConfiguration("udpConfiguration");

        host.Services.AddHttpClient("factoryServerHttpClient", (sProv, hClient) =>
            {
                var options = sProv.GetRequiredService<IOptions<HttpOptions>>();
                hClient.Timeout = options.Value.ConnectionTimeout;

                hClient.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(MediaTypeNames.Application.Json));
                hClient.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(MediaTypeNames.Application.Octet));

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
                        return NoValidationHandlerFunc;
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
            .AddSingleton<IFactoryServerHttpClientFactory, FactoryServerHttpClientFactory>()
            .AddSingleton<IFactoryServerClientFactory, FactoryServerClientFactory>();

        return host;
    }

}