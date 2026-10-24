using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace FactoryServerApiNew.Http;

public interface IServerCertificateValidationStrategy
{

    Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> CustomValidationCallback { get; }

}
