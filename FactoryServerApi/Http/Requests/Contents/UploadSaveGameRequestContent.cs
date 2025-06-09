namespace FactoryServerApi.Http.Requests.Contents;

internal class UploadSaveGameRequestContent : FactoryServerMultipartRequestContent
{
    public UploadSaveGameRequestContent(string saveName, bool loadSaveGame, bool enableAdvancedGameSettings, Stream saveFileStream)
        : base("UploadSaveGame", GetRequestContentData(saveName, loadSaveGame, enableAdvancedGameSettings), "saveGameFile", saveName, saveFileStream) { }

    private static FactoryServerRequestContentData GetRequestContentData(string saveName, bool loadSaveGame, bool enableAdvancedGameSettings)
    {
        Dictionary<string, object> data = new()
        {
            { "SaveName", saveName },
            { "LoadSaveGame", loadSaveGame },
            { "EnableAdvancedGameSettings", enableAdvancedGameSettings }
        };
        return new FactoryServerRequestContentData(data);
    }
}
