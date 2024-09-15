using System.Text.Json.Serialization;

namespace FactoryServerApi.Http.Responses;

public class QueryServerStateResponseData
{
    public ServerGameState ServerGameState { get; }

    [JsonConstructor]
    internal QueryServerStateResponseData(ServerGameState serverGameState)
    {
        ServerGameState = serverGameState;
    }
}
