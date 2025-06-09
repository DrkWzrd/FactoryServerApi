namespace FactoryServerApi.Http.Requests.Contents;

internal class SetAutoLoadSessionNameRequestContent : FactoryServerRequestContent
{
    public SetAutoLoadSessionNameRequestContent(string sessionName) : base("SetAutoLoadSessionName")
    {
        Data = new FactoryServerRequestContentData("SessionName", sessionName);
    }
}
