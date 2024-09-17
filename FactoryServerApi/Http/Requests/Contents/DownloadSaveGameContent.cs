namespace FactoryServerApi.Http.Requests.Contents;

internal class DownloadSaveGameContent : FactoryServerContent
{
    public DownloadSaveGameContent(string saveName) : base("DownloadSaveGame")
    {
        Data = new FactoryServerContentData("SaveName", saveName);
    }
}
