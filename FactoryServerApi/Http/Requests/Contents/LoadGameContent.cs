namespace FactoryServerApi.Http.Requests.Contents;

internal class LoadGameContent : FactoryServerContent
{
    public LoadGameContent(string saveName, bool enableAdvancedGameSettings) : base("LoadGame")
    {
        var data = new Dictionary<string, object?>
        {
            { "SaveName", saveName },
            { "EnableAdvancedGameSettings", enableAdvancedGameSettings }
        };
        Data = new FactoryServerContentData(data);
    }
}
