namespace FactoryServerApi.Http;

public class FactoryServerConnectionInfo
{

    public string Host { get; }

    public int Port { get; }

    public string? ApiPath { get; }

    public string? AuthenticationToken { get; set; }

    public FactoryGamePlayerId? PlayerId { get; set; }

    public FactoryServerConnectionInfo(string host, int port, string? apiPath = null)
    {
        Port = port;
        Host = host;
        ApiPath = apiPath;
    }

    public Uri GetUrl()
    {
        return new Uri($"{Host}:{Port}");
    }
}
