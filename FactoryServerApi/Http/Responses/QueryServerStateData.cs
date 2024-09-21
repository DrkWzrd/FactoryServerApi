namespace FactoryServerApi.Http.Responses;

public class QueryServerStateData : FactoryServerResponseContentData
{
    public ServerGameState ServerGameState { get; init; }
}
