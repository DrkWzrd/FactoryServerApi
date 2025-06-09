using System.Text.Json.Serialization;

namespace FactoryServerApi;

public class ServerNewGameData
{
    public string SessionName { get; }
    public MapName? MapName { get; set; }
    public StartingLocation? StartingLocation { get; set; }

    [JsonPropertyName("bSkipOnboarding")]
    public bool SkipOnboarding { get; set; } = true;
    public Dictionary<string, string> AdvancedGameSettings { get; }
    public Dictionary<string, string> CustomOptionsOnlyForModding { get; }

    public ServerNewGameData(string sessionName)
    {
        SessionName = sessionName;
        AdvancedGameSettings = [];
        CustomOptionsOnlyForModding = [];
    }

    public ServerNewGameData(string sessionName, MapName? mapName, StartingLocation? startingLocation, bool skipOnboarding)
        : this(sessionName, mapName, startingLocation, skipOnboarding, [], []) { }

    public ServerNewGameData(
        string sessionName,
        MapName? mapName,
        StartingLocation? startingLocation,
        bool skipOnboarding,
        Dictionary<string, string> advancedGameSettings,
        Dictionary<string, string> customOptionsOnlyForModding)
    {
        SessionName = sessionName;
        MapName = mapName;
        StartingLocation = startingLocation;
        SkipOnboarding = skipOnboarding;
        AdvancedGameSettings = advancedGameSettings;
        CustomOptionsOnlyForModding = customOptionsOnlyForModding;
    }
}
