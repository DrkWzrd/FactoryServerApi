namespace FactoryServerApi.Http.Requests.Contents;

internal class LoadGameRequestContent : FactoryServerRequestContent
{
    public LoadGameRequestContent(string saveName, bool enableAdvancedGameSettings) : base("LoadGame")
    {
        var data = new Dictionary<string, object>
        {
            { "SaveName", saveName },
            { "EnableAdvancedGameSettings", enableAdvancedGameSettings }
        };
        Data = new FactoryServerRequestContentData(data);
    }
}
