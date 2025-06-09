namespace FactoryServerApi.Http.Responses;

public class GetAdvancedGameSettingsData : FactoryServerResponseContentData
{
    public bool CreativeModeEnabled { get; init; }
    public IReadOnlyDictionary<string, string> AdvancedGameSettings { get; }

    public GetAdvancedGameSettingsData(IReadOnlyDictionary<string, string> advancedGameSettings)
    {
        AdvancedGameSettings = advancedGameSettings;
    }
}
