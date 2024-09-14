namespace FactoryServerApi.Http;

public class ServerGameState
{
    public string ActiveSessionName { get; }
    public int NumConnectedPlayers { get; }
    public int PlayerLimit { get; }
    public int TechTier { get; }
    public string ActiveSchematic { get; }
    public string GamePhase { get; }
    public bool IsGameRunning { get; }
    public int TotalGameDuration { get; }
    public bool IsGamePaused { get; }
    public float AverageTickRate { get; }
    public string AutoLoadSessionName { get; }

    internal ServerGameState(
        string activeSessionName,
        int numConnectedPlayers,
        int playerLimit,
        int techTier,
        string activeSchematic,
        string gamePhase,
        bool isGameRunning,
        int totalGameDuration,
        bool isGamePaused,
        float averageTickRate,
        string autoLoadSessionName)
    {
        ActiveSessionName = activeSessionName;
        NumConnectedPlayers = numConnectedPlayers;
        PlayerLimit = playerLimit;
        TechTier = techTier;
        ActiveSchematic = activeSchematic;
        GamePhase = gamePhase;
        IsGameRunning = isGameRunning;
        TotalGameDuration = totalGameDuration;
        IsGamePaused = isGamePaused;
        AverageTickRate = averageTickRate;
        AutoLoadSessionName = autoLoadSessionName;
    }
}