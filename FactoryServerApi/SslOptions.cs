
using FactoryServerApi.Http;

namespace FactoryServerApi;

public class SslOptions
{
    // NOTE: NoValidation accepts self-signed certificates without checking them,
    // which exposes the connection to man-in-the-middle attacks. See the README.
    public SslValidationStrategy ServerCertificateValidationStrategy { get; set; } = SslValidationStrategy.NoValidation;
}
