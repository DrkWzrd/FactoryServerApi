namespace FactoryServerApi.Http.Requests.Contents;

internal class UploadSaveGameContent : FactoryServerMultipartContent
{
    public UploadSaveGameContent(string saveName, bool loadSaveGame, bool enableAdvancedGameSettings, Stream saveFileStream)
        : base(
            "UploadSaveGame",
            new FactoryServerContentData(
                new Dictionary<string, object?>
                {
                    { "SaveName", saveName },
                    { "LoadSaveGame", loadSaveGame },
                    { "EnableAdvancedGameSettings", enableAdvancedGameSettings }
                }),
            "saveGameFile",
            saveName,
            saveFileStream)
    {
    }
}
