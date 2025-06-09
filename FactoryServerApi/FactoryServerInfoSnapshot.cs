namespace FactoryServerApi;

public class FactoryServerInfoSnapshot
{
    public bool? IsClaimed { get; }
    public FactoryServerState ServerState { get; }
    public FactoryServerFlags? Flags { get; }
    public uint? ChangeList { get; }
    public string? ServerName { get; }
    public FactoryServerHealthState? HealthState { get; }
    public string? ServerCustomData { get; }
    public ServerGameState? GameState { get; }
    public IReadOnlyDictionary<string, string> CurrentOptions { get; }
    public IReadOnlyDictionary<string, string> PendingForRestartOptions { get; }
    public IReadOnlyDictionary<string, string> AdvancedGameSettings { get; }
    public bool? IsCreativeModeEnabled { get; }
    public IReadOnlyList<FactorySaveSession> Sessions { get; }
    public int CurrentSessionIndex { get; }

    internal FactoryServerInfoSnapshot(FactoryServerInfo serverInfo)
    {
        IsClaimed = serverInfo.IsClaimed;
        ServerState = serverInfo.ServerState;
        Flags = serverInfo.Flags;
        ChangeList = serverInfo.ChangeList;
        ServerName = serverInfo.ServerName;
        HealthState = serverInfo.HealthState;
        ServerCustomData = serverInfo.ServerCustomData;
        GameState = serverInfo.GameState;
        CurrentOptions = serverInfo.CurrentOptions.AsReadOnly();
        PendingForRestartOptions = serverInfo.PendingForRestartOptions.AsReadOnly();
        AdvancedGameSettings = serverInfo.AdvancedGameSettings.AsReadOnly();
        IsCreativeModeEnabled = serverInfo.IsCreativeModeEnabled;
        Sessions = serverInfo.Sessions.AsReadOnly();
        CurrentSessionIndex = serverInfo.CurrentSessionIndex;
    }
}