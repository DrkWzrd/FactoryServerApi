using FactoryServerApi.Http;
using FactoryServerApi.Http.Responses;
using FactoryServerApi.Udp;

namespace FactoryServerApi;

public class FactoryServerInfo
{
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

    public SessionSaveStruct CurrentSession => _sessions[_currentSessionIndex];

    public bool CreativeModeEnabled { get; private set; }

    //public ulong LastSeenCookie { get; private set; }

    public FactoryServerInfo(
        string serverName,
        //ulong cookie,
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
        //LastSeenCookie = cookie;
        _serverState = serverState;
        Flags = flags;
        ChangeList = serverChangeList;
        _gameState = gameState;
        CreativeModeEnabled = creativeModeEnabled;
        _currentOptions = options;
        _pendingForRestartOptions = advancedSettings;
        _advancedGameSettings = pendingOptions;
        _sessions = sessions;
        _currentSessionIndex = currentSessionIndex;
    }

    internal void UpdateValue(GetAdvancedGameSettingsResponseData advancedGameSettings, ulong cookie = ulong.MinValue)
    {
        CreativeModeEnabled = advancedGameSettings.CreativeModeEnabled;
        _advancedGameSettings = advancedGameSettings.AdvancedGameSettings;
        //LastSeenCookie = cookie;
        Version++;
    }

    internal void UpdateValue(GetServerOptionsResponseData options, ulong cookie = ulong.MinValue)
    {
        _currentOptions = options.ServerOptions;
        _pendingForRestartOptions = options.PendingServerOptions;
        //LastSeenCookie = cookie;
        Version++;
    }

    internal void UpdateValue(EnumerateSessionsResponseData sessions, ulong cookie = ulong.MinValue)
    {
        _sessions = sessions.Sessions;
        _currentSessionIndex = sessions.CurrentSessionIndex;
        //LastSeenCookie = cookie;
        Version++;
    }

    internal void UpdateValue(FactoryServerStateResponse response, HealthCheckResponseData health)
    {
        _serverState = response.ServerState;
        _serverName = response.ServerName;
        HealthState = health.Health;
        ServerCustomData = health.ServerCustomData;
        //LastSeenCookie = cookie;
        Version++;
    }
}
