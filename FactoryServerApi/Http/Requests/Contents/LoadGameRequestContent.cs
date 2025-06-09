namespace FactoryServerApi.Http.Requests.Contents;

internal class LoadGameRequestContent : FactoryServerRequestContent
{
    public LoadGameRequestContent(string saveName, bool enableAdvancedGameSettings) : base("LoadGame")
    {
        Dictionary<string, object> data = new()
        {
            { "SaveName", saveName },
            { "EnableAdvancedGameSettings", enableAdvancedGameSettings }
        };
        Data = new FactoryServerRequestContentData(data);
    }
}
