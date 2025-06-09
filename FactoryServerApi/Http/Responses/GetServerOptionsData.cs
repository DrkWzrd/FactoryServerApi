namespace FactoryServerApi.Http.Responses;

public class GetServerOptionsData : FactoryServerResponseContentData
{
    public IReadOnlyDictionary<string, string> ServerOptions { get; }

    public GetServerOptionsData(IReadOnlyDictionary<string, string> serverOptions, IReadOnlyDictionary<string, string> pendingServerOptions)
    {
        ServerOptions = serverOptions;
        PendingServerOptions = pendingServerOptions;
    }

    public IReadOnlyDictionary<string, string> PendingServerOptions { get; }
}
