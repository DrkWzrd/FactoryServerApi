using FactoryServerApi.Http;

namespace FactoryServerApi;

public class SslOptions
{
    public SslValidationStrategy ServerCertificateValidationStrategy { get; init; }
}
