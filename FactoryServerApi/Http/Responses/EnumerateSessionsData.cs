namespace FactoryServerApi.Http.Responses;

public class EnumerateSessionsData : FactoryServerResponseContentData
{
    public IReadOnlyList<SessionSaveStruct> Sessions { get; init; }
    public int CurrentSessionIndex { get; init; }
}
