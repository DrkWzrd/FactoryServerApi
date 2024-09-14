namespace FactoryServerApi.Http.Requests.Contents;

internal class DeleteSaveFileContent : FactoryServerContent
{
    public DeleteSaveFileContent(string saveName) : base("DeleteSaveFile")
    {
        Data = new SinglePropertyFactoryServerContentData("SaveName", saveName);
    }
}
