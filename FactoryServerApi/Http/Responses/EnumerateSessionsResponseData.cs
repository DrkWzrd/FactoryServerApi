namespace FactoryServerApi.Http.Responses;

public class EnumerateSessionsResponseData
{
    public IReadOnlyList<SessionSaveStruct> Sessions { get; }
    public int CurrentSessionIndex { get; }

    internal EnumerateSessionsResponseData(
        List<SessionSaveStruct> sessions,
        int currentSessionIndex)
    {
        Sessions = sessions;
        CurrentSessionIndex = currentSessionIndex;
    }
}
