namespace FactoryServerApi.Http.Responses;

public class QueryServerStateResponseData
{
    public ServerGameState ServerGameState { get; }

    internal QueryServerStateResponseData(ServerGameState serverGameState)
    {
        ServerGameState = serverGameState;
    }
}
