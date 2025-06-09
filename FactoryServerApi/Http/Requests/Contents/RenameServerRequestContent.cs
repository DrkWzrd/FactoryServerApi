namespace FactoryServerApi.Http.Requests.Contents;

internal class RenameServerRequestContent : FactoryServerRequestContent
{

    public RenameServerRequestContent(string serverName) : base("RenameServer")
    {
        Data = new FactoryServerRequestContentData("ServerName", serverName);
    }

}
