using FactoryServerApi.Http.Responses;
using FactoryServerApi.Udp;

namespace FactoryServerApi;

internal class FactoryServerInfo
{

    public bool? IsClaimed { get; set; }


    public FactoryServerState ServerState { get; private set; } = FactoryServerState.Offline;

    public FactoryServerFlags? Flags { get; private set; }

    public uint? ChangeList { get; private set; }

    public string? ServerName { get; private set; }


    public FactoryServerHealthState? HealthState { get; private set; }

    public string? ServerCustomData { get; private set; }


    public ServerGameState? GameState { get; private set; }


    public Dictionary<string, string> CurrentOptions { get; private set; } = [];

    public Dictionary<string, string> PendingForRestartOptions { get; private set; } = [];


    public Dictionary<string, string> AdvancedGameSettings { get; private set; } = [];

    public bool? IsCreativeModeEnabled { get; private set; }


    public List<FactorySaveSession> Sessions { get; private set; } = [];

    public int CurrentSessionIndex { get; private set; } = -1;


    public Dictionary<string, object?> CustomStatesData { get; private set; } = [];

    internal FactoryServerInfo() { }

    public void UpdateValue(FactoryServerStateUdpResponse serverStateResponse)
    {
        ServerState = serverStateResponse.ServerState;
        Flags = serverStateResponse.ServerFlags;
        ChangeList = serverStateResponse.ServerNetCL;
        ServerName = serverStateResponse.ServerName;
    }

    public void UpdateValue(HealthCheckData healthData)
    {
        HealthState = healthData.Health;
        ServerCustomData = healthData.ServerCustomData;
    }

    public void UpdateValue(QueryServerStateData serverStateData)
    {
        GameState = serverStateData.ServerGameState;
    }

    public void UpdateValue(GetAdvancedGameSettingsData advancedGameSettingsData)
    {
        IsCreativeModeEnabled = advancedGameSettingsData.CreativeModeEnabled;
        AdvancedGameSettings = new(advancedGameSettingsData.AdvancedGameSettings);
    }

    public void UpdateValue(GetServerOptionsData optionsData)
    {
        CurrentOptions = new(optionsData.ServerOptions);
        PendingForRestartOptions = new(optionsData.PendingServerOptions);
    }

    public void UpdateValue(EnumerateSessionsData sessions)
    {
        Sessions = new(sessions.Sessions);
        CurrentSessionIndex = sessions.CurrentSessionIndex;
    }

    public void UpdateValue(IReadOnlyDictionary<string, object?> customStatesData)
    {
        CustomStatesData = new(customStatesData);
    }
}

//internal class FactoryServerInfo
//{

//    public static readonly FactoryServerInfo Offline = new();

//    private ServerGameState _gameState;
//    private FactoryServerState _serverState;
//    private IReadOnlyDictionary<string, string> _advancedGameSettings;
//    private IReadOnlyDictionary<string, string> _pendingForRestartOptions;
//    private IReadOnlyDictionary<string, string> _currentOptions;
//    private IReadOnlyList<FactorySaveSession> _sessions;
//    private int _currentSessionIndex;
//    private string _serverName;

//    public int Version { get; private set; }

//    public bool IsServerClaimed { get; private set; }

//    public string ServerName => _serverName;

//    public ServerGameState GameState
//    {
//        get => _gameState;
//        set
//        {
//            _gameState = value;
//            Version++;
//        }
//    }

//    public FactoryServerState ServerState => _serverState;

//    public FactoryServerHealthState HealthState { get; private set; }

//    public string? ServerCustomData { get; private set; }

//    public FactoryServerFlags Flags { get; }

//    public uint ChangeList { get; }

//    public IReadOnlyDictionary<string, string> CurrentOptions => _currentOptions;

//    public IReadOnlyDictionary<string, string> PendingForRestartOptions => _pendingForRestartOptions;

//    public IReadOnlyDictionary<string, string> AdvancedGameSettings => _advancedGameSettings;

//    public IReadOnlyList<FactorySaveSession> Sessions => _sessions;

//    [JsonIgnore]
//    public FactorySaveSession CurrentSession => _sessions[_currentSessionIndex];

//    public bool CreativeModeEnabled { get; private set; }

//    public bool IsOffline => _serverState == FactoryServerState.Offline;

//    public

//    private FactoryServerInfo()
//    {
//        _serverState = FactoryServerState.Offline;
//    }

//    public FactoryServerInfo(
//        string serverName,
//        FactoryServerState serverState,
//        FactoryServerFlags flags,
//        uint serverChangeList,
//        FactoryServerHealthState health,
//        string? serverCustomData,
//        ServerGameState gameState,
//        IReadOnlyDictionary<string, string> options,
//        IReadOnlyDictionary<string, string> pendingOptions,
//        bool creativeModeEnabled,
//        IReadOnlyDictionary<string, string> advancedSettings,
//        IReadOnlyList<FactorySaveSession> sessions,
//        int currentSessionIndex)
//    {
//        _serverName = serverName;
//        _serverState = serverState;
//        Flags = flags;
//        ChangeList = serverChangeList;
//        _gameState = gameState;
//        CreativeModeEnabled = creativeModeEnabled;
//        _currentOptions = options;
//        _pendingForRestartOptions = pendingOptions;
//        _advancedGameSettings = advancedSettings;
//        _sessions = sessions;
//        _currentSessionIndex = currentSessionIndex;
//    }

//    public void UpdateValue(GetAdvancedGameSettingsData advancedGameSettings)
//    {
//        CreativeModeEnabled = advancedGameSettings.CreativeModeEnabled;
//        _advancedGameSettings = advancedGameSettings.AdvancedGameSettings;
//        Version++;
//    }

//    public void UpdateValue(GetServerOptionsData options)
//    {
//        _currentOptions = options.ServerOptions;
//        _pendingForRestartOptions = options.PendingServerOptions;
//        Version++;
//    }

//    public void UpdateValue(EnumerateSessionsData sessions)
//    {
//        _sessions = sessions.Sessions;
//        _currentSessionIndex = sessions.CurrentSessionIndex;
//        Version++;
//    }

//    public void UpdateValue(FactoryServerStateUdpResponse response, HealthCheckData health)
//    {
//        _serverState = response.ServerState;
//        _serverName = response.ServerName;
//        HealthState = health.Health;
//        ServerCustomData = health.ServerCustomData;
//        Version++;
//    }

//    public FactoryServerInfoSnapshot GetSnapshot()
//    {

//        var json = JsonSerializer.Serialize(this);
//        return JsonSerializer.Deserialize<FactoryServerInfoSnapshot>(json) ?? throw new JsonException();
//    }
//}
