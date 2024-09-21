namespace FactoryServerApi.Http.Requests.Contents;

internal class ApplyAdvancedGameSettingsRequestContent : FactoryServerRequestContent
{

    public ApplyAdvancedGameSettingsRequestContent(IDictionary<string, string> settings) : base("ApplyAdvancedGameSettings")
    {
        Data = new FactoryServerRequestContentData("AppliedAdvancedGameSettings", settings);
    }

}
