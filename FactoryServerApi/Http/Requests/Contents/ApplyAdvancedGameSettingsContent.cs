namespace FactoryServerApi.Http.Requests.Contents;

internal class ApplyAdvancedGameSettingsContent : FactoryServerContent
{

    public ApplyAdvancedGameSettingsContent(IDictionary<string, string> settings) : base("ApplyAdvancedGameSettings")
    {
        Data = new FactoryServerContentData("AppliedAdvancedGameSettings", settings);
    }

}
