namespace FactoryServerApi.Http.Requests.Contents;

internal class DeleteSaveSessionRequestContent : FactoryServerRequestContent
{
    public DeleteSaveSessionRequestContent(string sessionName) : base("DeleteSaveSession")
    {
        Data = new FactoryServerRequestContentData("SessionName", sessionName);
    }
}
