namespace FactoryServerApi.Http.Requests.Contents;

internal class RenameServerContent : FactoryServerContent
{

    public RenameServerContent(string serverName) : base("RenameServer")
    {
        Data = new FactoryServerContentData("ServerName", serverName);
    }

}
