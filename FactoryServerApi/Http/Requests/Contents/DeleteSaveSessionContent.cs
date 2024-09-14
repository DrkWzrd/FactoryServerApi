namespace FactoryServerApi.Http.Requests.Contents;

internal class DeleteSaveSessionContent : FactoryServerContent
{
    public DeleteSaveSessionContent(string sessionName) : base("DeleteSaveSession")
    {
        Data = new SinglePropertyFactoryServerContentData("SessionName", sessionName);
    }
}
