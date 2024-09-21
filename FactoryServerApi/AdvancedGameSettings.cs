namespace FactoryServerApi;

//public class AdvancedGameSettings
//{
//    private readonly ServerSettings _sSettings = new();
//    private readonly PlayerSettings _pSettings = new();

//    [JsonIgnore]
//    public ServerSettings ServerSettings => _sSettings;

//    [JsonIgnore]
//    public PlayerSettings PlayerSettings => _pSettings;

//    [JsonInclude]
//    [JsonPropertyName("FG.GameRules.NoPower")]
//    public bool NoPower
//    {
//        get => _sSettings.NoPower;
//        set => _sSettings.NoPower = value;
//    }

//    [JsonInclude]
//    [JsonPropertyName("FG.GameRules.DisableArachnidCreatures")]
//    public bool DisableArachnidCreatures
//    {
//        get => _sSettings.DisableArachnidCreatures;
//        set => _sSettings.DisableArachnidCreatures = value;
//    }

//    [JsonInclude]
//    [JsonPropertyName("FG.GameRules.NoUnlockCost")]
//    public bool NoUnlockCost
//    {
//        get => _sSettings.NoUnlockCost;
//        set => _sSettings.NoUnlockCost = value;
//    }

//    [JsonInclude]
//    [JsonPropertyName("FG.GameRules.SetGamePhase")]
//    public int SetGamePhase
//    {
//        get => _sSettings.SetGamePhase;
//        set => _sSettings.SetGamePhase = value;
//    }

//    [JsonInclude]
//    [JsonPropertyName("FG.GameRules.GiveAllTiers")]
//    public bool GiveAllTiers
//    {
//        get => _sSettings.GiveAllTiers;
//        set => _sSettings.GiveAllTiers = value;
//    }

//    [JsonInclude]
//    [JsonPropertyName("FG.GameRules.UnlockAllResearchSchematics")]
//    public bool UnlockAllResearchSchematics
//    {
//        get => _sSettings.UnlockAllResearchSchematics;
//        set => _sSettings.UnlockAllResearchSchematics = value;
//    }

//    [JsonInclude]
//    [JsonPropertyName("FG.GameRules.UnlockInstantAltRecipes")]
//    public bool UnlockInstantAltRecipes
//    {
//        get => _sSettings.UnlockInstantAltRecipes;
//        set => _sSettings.UnlockInstantAltRecipes = value;
//    }

//    [JsonInclude]
//    [JsonPropertyName("FG.GameRules.UnlockAllResourceSinkSchematics")]
//    public bool UnlockAllResourceSinkSchematics
//    {
//        get => _sSettings.UnlockAllResourceSinkSchematics;
//        set => _sSettings.UnlockAllResourceSinkSchematics = value;
//    }

//    [JsonInclude]
//    [JsonPropertyName("FG.GameRules.GiveItems")]
//    public string GiveItems
//    {
//        get => _sSettings.GiveItems;
//        set => _sSettings.GiveItems = value;
//    }

//    [JsonInclude]
//    [JsonPropertyName("FG.PlayerRules.NoBuildCost")]
//    public bool NoBuildCost
//    {
//        get => _pSettings.NoBuildCost;
//        set => _pSettings.NoBuildCost = value;
//    }

//    [JsonInclude]
//    [JsonPropertyName("FG.PlayerRules.GodMode")]
//    public bool GodMode
//    {
//        get => _pSettings.GodMode;
//        set => _pSettings.GodMode = value;
//    }

//    [JsonInclude]
//    [JsonPropertyName("FG.PlayerRules.FlightMode")]
//    public bool FlightMode
//    {
//        get => _pSettings.FlightMode;
//        set => _pSettings.FlightMode = value;
//    }
//}
