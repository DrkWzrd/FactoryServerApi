namespace FactoryServerApi.Http.Responses;

public class EnumerateSessionsData : FactoryServerResponseContentData
{
    public IReadOnlyList<FactorySaveSession> Sessions { get; init; }

    public int CurrentSessionIndex { get; init; }

}
