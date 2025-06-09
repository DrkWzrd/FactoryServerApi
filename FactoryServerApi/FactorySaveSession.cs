using System.Text.Json.Serialization;

namespace FactoryServerApi;

public class FactorySaveSession
{
    public string SessionName { get; }
    public IReadOnlyList<FactorySaveFile> SaveHeaders { get; }

    [JsonConstructor]
    internal FactorySaveSession(
        string sessionName,
        IReadOnlyList<FactorySaveFile> saveHeaders)
    {
        SessionName = sessionName;
        SaveHeaders = saveHeaders;
    }
}
