using System.Text.Json.Serialization;

namespace FactoryServerApi.Http.Responses;

public class GetServerOptionsResponseData
{
    public IReadOnlyDictionary<string, string> ServerOptions { get; }
    public IReadOnlyDictionary<string, string> PendingServerOptions { get; }

    [JsonConstructor]
    internal GetServerOptionsResponseData(
        Dictionary<string, string> serverOptions,
        Dictionary<string, string> pendingServerOptions)
    {
        ServerOptions = serverOptions;
        PendingServerOptions = pendingServerOptions;
    }
}
