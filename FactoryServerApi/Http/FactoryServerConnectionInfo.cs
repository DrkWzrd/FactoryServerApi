namespace FactoryServerApi.Http;

public class FactoryServerConnectionInfo
{

    public string Host { get; }

    public int Port { get; }

    public string? AuthenticationToken { get; set; }

    public FactoryServerConnectionInfo(string host, int port)
    {
        Port = port;
        Host = host;
    }

    public Uri GetUrl()
    {
        return new Uri($"{Host}:{Port}");
    }
}
