using System.Text.Json.Serialization;

namespace FactoryServerApi.Http.Responses;

public class EnumerateSessionsResponseData
{
    public IReadOnlyList<SessionSaveStruct> Sessions { get; }
    public int CurrentSessionIndex { get; }

    [JsonConstructor]
    internal EnumerateSessionsResponseData(
        IReadOnlyList<SessionSaveStruct> sessions,
        int currentSessionIndex)
    {
        Sessions = sessions;
        CurrentSessionIndex = currentSessionIndex;
    }
}
