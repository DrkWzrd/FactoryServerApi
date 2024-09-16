namespace FactoryServerApi.Http;

public record FactoryServerConnectionInfo(string Host, int Port, string? ApiPath, string? AuthenticationToken, FactoryGamePlayerId? PlayerId)
{
    public FactoryServerConnectionInfo(string host, int port, string? apiPath = null) : this(host, port, apiPath, null, null)
    {
    }

    public Uri GetUrl()
    {
        return new Uri($"{Host}:{Port}");
    }
}
