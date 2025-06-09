namespace FactoryServerApi;

public class ServerGameState
{

    public string ActiveSessionName { get; } = default!;
    public int NumConnectedPlayers { get; }
    public int PlayerLimit { get; }
    public int TechTier { get; }
    public string ActiveSchematic { get; } = default!;
    public string GamePhase { get; } = default!;
    public bool IsGameRunning { get; }
    public int TotalGameDuration { get; }
    public bool IsGamePaused { get; }
    public float AverageTickRate { get; }
    public string AutoLoadSessionName { get; } = default!;

}