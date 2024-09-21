using System.Text.Json.Serialization;

namespace FactoryServerApi;

public class SessionSaveStruct
{
    public string SessionName { get; }
    public IReadOnlyList<SaveHeader> SaveHeaders { get; }

    [JsonConstructor]
    internal SessionSaveStruct(
        string sessionName,
        IReadOnlyList<SaveHeader> saveHeaders)
    {
        SessionName = sessionName;
        SaveHeaders = saveHeaders;
    }
}
