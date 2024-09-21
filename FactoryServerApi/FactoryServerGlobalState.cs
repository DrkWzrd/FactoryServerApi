using FactoryServerApi.Http.Responses;
using FactoryServerApi.Udp;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FactoryServerApi;

public class FactoryServerGlobalState
{

    public static readonly FactoryServerGlobalState Offline = new();

    private ServerGameState _gameState;
    private FactoryServerState _serverState;
    private IReadOnlyDictionary<string, string> _advancedGameSettings;
    private IReadOnlyDictionary<string, string> _pendingForRestartOptions;
    private IReadOnlyDictionary<string, string> _currentOptions;
    private IReadOnlyList<SessionSaveStruct> _sessions;
    private int _currentSessionIndex;
    private string _serverName;

    internal int Version { get; private set; }

    public string ServerName => _serverName;

    public ServerGameState GameState
    {
        get => _gameState;
        internal set
        {
            _gameState = value;
            Version++;
        }
    }

    public FactoryServerState ServerState => _serverState;

    public FactoryServerHealthState HealthState { get; private set; }

    public string? ServerCustomData { get; private set; }

    public FactoryServerFlags Flags { get; }

    public uint ChangeList { get; }

    public IReadOnlyDictionary<string, string> CurrentOptions => _currentOptions;

    public IReadOnlyDictionary<string, string> PendingForRestartOptions => _pendingForRestartOptions;

    public IReadOnlyDictionary<string, string> AdvancedGameSettings => _advancedGameSettings;

    public IReadOnlyList<SessionSaveStruct> Sessions => _sessions;

    [JsonIgnore]
    public SessionSaveStruct CurrentSession => _sessions[_currentSessionIndex];

    public bool CreativeModeEnabled { get; private set; }

    public bool IsOffline => _serverState == FactoryServerState.Offline;

    private FactoryServerGlobalState()
    {
        _serverState = FactoryServerState.Offline;
    }

    public FactoryServerGlobalState(
        string serverName,
        FactoryServerState serverState,
        FactoryServerFlags flags,
        uint serverChangeList,
        FactoryServerHealthState health,
        string? serverCustomData,
        ServerGameState gameState,
        IReadOnlyDictionary<string, string> options,
        IReadOnlyDictionary<string, string> pendingOptions,
        bool creativeModeEnabled,
        IReadOnlyDictionary<string, string> advancedSettings,
        IReadOnlyList<SessionSaveStruct> sessions,
        int currentSessionIndex)
    {
        _serverName = serverName;
        _serverState = serverState;
        Flags = flags;
        ChangeList = serverChangeList;
        _gameState = gameState;
        CreativeModeEnabled = creativeModeEnabled;
        _currentOptions = options;
        _pendingForRestartOptions = pendingOptions;
        _advancedGameSettings = advancedSettings;
        _sessions = sessions;
        _currentSessionIndex = currentSessionIndex;
    }

    internal void UpdateValue(GetAdvancedGameSettingsData advancedGameSettings)
    {
        CreativeModeEnabled = advancedGameSettings.CreativeModeEnabled;
        _advancedGameSettings = advancedGameSettings.AdvancedGameSettings;
        Version++;
    }

    internal void UpdateValue(GetServerOptionsData options)
    {
        _currentOptions = options.ServerOptions;
        _pendingForRestartOptions = options.PendingServerOptions;
        Version++;
    }

    internal void UpdateValue(EnumerateSessionsData sessions)
    {
        _sessions = sessions.Sessions;
        _currentSessionIndex = sessions.CurrentSessionIndex;
        Version++;
    }

    internal void UpdateValue(FactoryServerStateUdpResponse response, HealthCheckData health)
    {
        _serverState = response.ServerState;
        _serverName = response.ServerName;
        HealthState = health.Health;
        ServerCustomData = health.ServerCustomData;
        Version++;
    }

    internal FactoryServerStateSnapshot GetSnapshot()
    {
        var json = JsonSerializer.Serialize(this);
        return JsonSerializer.Deserialize<FactoryServerStateSnapshot>(json) ?? throw new JsonException();
    }
}
