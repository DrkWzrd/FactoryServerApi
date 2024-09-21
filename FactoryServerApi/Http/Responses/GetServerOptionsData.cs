namespace FactoryServerApi.Http.Responses;

public class GetServerOptionsData : FactoryServerResponseContentData
{
    public IReadOnlyDictionary<string, string> ServerOptions { get; init; }
    public IReadOnlyDictionary<string, string> PendingServerOptions { get; init; }
}
