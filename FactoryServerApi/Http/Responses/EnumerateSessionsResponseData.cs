using System.Text.Json.Serialization;

namespace FactoryServerApi.Http.Responses;

public class EnumerateSessionsResponseData
{
    public IReadOnlyList<SessionSaveStruct> Sessions { get; }
    public int CurrentSessionIndex { get; }

    //[JsonIgnore]
    //public SessionSaveStruct? CurrentSession => CurrentSessionIndex < 0 ? null : Sessions[CurrentSessionIndex];

    [JsonConstructor]
    internal EnumerateSessionsResponseData(
        IReadOnlyList<SessionSaveStruct> sessions,
        int currentSessionIndex)
    {
        Sessions = sessions;
        CurrentSessionIndex = currentSessionIndex;
    }
}
