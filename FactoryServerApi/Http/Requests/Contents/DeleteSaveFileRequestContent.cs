namespace FactoryServerApi.Http.Requests.Contents;

internal class DeleteSaveFileRequestContent : FactoryServerRequestContent
{
    public DeleteSaveFileRequestContent(string saveName) : base("DeleteSaveFile")
    {
        Data = new FactoryServerRequestContentData("SaveName", saveName);
    }
}
