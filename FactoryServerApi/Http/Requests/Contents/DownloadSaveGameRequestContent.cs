namespace FactoryServerApi.Http.Requests.Contents;

internal class DownloadSaveGameRequestContent : FactoryServerRequestContent
{
    public DownloadSaveGameRequestContent(string saveName) : base("DownloadSaveGame")
    {
        Data = new FactoryServerRequestContentData("SaveName", saveName);
    }
}
