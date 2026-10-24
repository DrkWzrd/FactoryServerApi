using System.Text.Json.Serialization;

namespace FactoryServerApiNew;

public class ServerNewGameData
{
    public string SessionName { get; }

    public MapName? MapName { get; set; }
    public StartingLocation? StartingLocation { get; set; }

    // The docs say the field is "SkipOnboarding", but the server actually expects "bSkipOnboarding".
    [JsonPropertyName("bSkipOnboarding")]
    public bool SkipOnboarding { get; set; } = true;
    public Dictionary<string, string> AdvancedGameSettings { get; }
    public Dictionary<string, string> CustomOptionsOnlyForModding { get; }

    // Not documented in the API docs, but the live server rejects the request
    // without this field ("Missing JSON value named GameModeSettings").
    public Dictionary<string, string> GameModeSettings { get; }

    public ServerNewGameData(string sessionName)
    {
        SessionName = sessionName;
        AdvancedGameSettings = [];
        CustomOptionsOnlyForModding = [];
        GameModeSettings = [];
    }

    public ServerNewGameData(string sessionName, MapName? mapName, StartingLocation? startingLocation, bool skipOnboarding)
        : this(sessionName, mapName, startingLocation, skipOnboarding, [], [], []) { }

    public ServerNewGameData(
        string sessionName,
        MapName? mapName,
        StartingLocation? startingLocation,
        bool skipOnboarding,
        Dictionary<string, string> advancedGameSettings,
        Dictionary<string, string> customOptionsOnlyForModding,
        Dictionary<string, string> gameModeSettings)
    {
        SessionName = sessionName;
        MapName = mapName;
        StartingLocation = startingLocation;
        SkipOnboarding = skipOnboarding;
        AdvancedGameSettings = advancedGameSettings;
        CustomOptionsOnlyForModding = customOptionsOnlyForModding;
        GameModeSettings = gameModeSettings;
    }
}
