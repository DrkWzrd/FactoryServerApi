using System.Text.Json.Serialization;

namespace FactoryServerApi;

public record ServerGameState
{

    public static readonly ServerGameState Unknown = new();

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

    private ServerGameState()
    {
        ActiveSessionName = string.Empty;
        NumConnectedPlayers = -1;
        PlayerLimit = -1;
        TechTier = -1;
        ActiveSchematic = string.Empty;
        GamePhase = string.Empty;
        IsGameRunning = false;
        TotalGameDuration = -1;
        IsGamePaused = false;
        AverageTickRate = -1;
        AutoLoadSessionName = string.Empty;
    }

    [JsonConstructor]
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