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

    public FactoryServerInfo() { }

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
        Sessions = [.. sessions.Sessions];
        CurrentSessionIndex = sessions.CurrentSessionIndex;
    }

    public void UpdateValue(IReadOnlyDictionary<string, object?> customStatesData)
    {
        CustomStatesData = new(customStatesData);
    }
}