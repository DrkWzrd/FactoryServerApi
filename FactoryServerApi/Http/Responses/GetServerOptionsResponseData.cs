using System.Text.Json.Serialization;

namespace FactoryServerApi.Http.Responses;

public class GetServerOptionsResponseData
{
    public IReadOnlyDictionary<string, string> ServerOptions { get; }
    public IReadOnlyDictionary<string, string> PendingServerOptions { get; }

    [JsonConstructor]
    internal GetServerOptionsResponseData(
        IReadOnlyDictionary<string, string> serverOptions,
        IReadOnlyDictionary<string, string> pendingServerOptions)
    {
        ServerOptions = serverOptions;
        PendingServerOptions = pendingServerOptions;
    }
}
