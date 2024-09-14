namespace FactoryServerApi.Http;

public class ServerNewGameData
{
    public string SessionName { get; }
    public string MapName { get; }
    public string StartingLocation { get; }
    public bool SkipOnboarding { get; }
    public Dictionary<string, string> AdvancedGameSettings { get; }
    public Dictionary<string, string> CustomOptionsOnlyForModding { get; }

    public ServerNewGameData(
        string sessionName,
        string mapName,
        string startingLocation,
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

    public ServerNewGameData(
        string sessionName,
        string mapName,
        string startingLocation,
        bool skipOnboarding)
    {
        SessionName = sessionName;
        MapName = mapName;
        StartingLocation = startingLocation;
        SkipOnboarding = skipOnboarding;
        AdvancedGameSettings = [];
        CustomOptionsOnlyForModding = [];
    }
}
