using System.Text.Json.Serialization;

namespace FactoryServerApi;

public record FactoryServerStateSnapshot(
    string ServerName,
    FactoryServerState ServerState,
    FactoryServerFlags Flags,
    uint ChangeList,
    FactoryServerHealthState HealthState,
    string? ServerCustomData,
    ServerGameState GameState,
    IReadOnlyDictionary<string, string> CurrentOptions,
    IReadOnlyDictionary<string, string> PendingForRestartOptions,
    IReadOnlyDictionary<string, string> AdvancedGameSettings,
    IReadOnlyList<SessionSaveStruct> Sessions,
    int CurrentSessionIndex,
    bool CreativeModeEnabled,
    DateTimeOffset TimeStamp)
{
    [JsonIgnore]
    public SessionSaveStruct CurrentSession => Sessions[CurrentSessionIndex];
    public bool IsOffline => ServerState == FactoryServerState.Offline;
}
