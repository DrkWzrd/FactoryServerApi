using System.Text.Json.Serialization;

namespace FactoryServerApi.Http.Responses;

public class EnumerateSessionsResponseData
{
    public IReadOnlyList<SessionSaveStruct> Sessions { get; }
    public int CurrentSessionIndex { get; }

    [JsonConstructor]
    internal EnumerateSessionsResponseData(
        List<SessionSaveStruct> sessions,
        int currentSessionIndex)
    {
        Sessions = sessions;
        CurrentSessionIndex = currentSessionIndex;
    }
}
