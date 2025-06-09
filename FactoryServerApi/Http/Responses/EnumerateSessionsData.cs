namespace FactoryServerApi.Http.Responses;

public class EnumerateSessionsData : FactoryServerResponseContentData
{
    public IReadOnlyList<FactorySaveSession> Sessions { get; }

    public EnumerateSessionsData(IReadOnlyList<FactorySaveSession> sessions)
    {
        Sessions = sessions;
    }

    public int CurrentSessionIndex { get; init; }

}
