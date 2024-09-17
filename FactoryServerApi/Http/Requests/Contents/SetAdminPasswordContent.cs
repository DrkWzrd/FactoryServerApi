namespace FactoryServerApi.Http.Requests.Contents;

internal class SetAdminPasswordContent : FactoryServerContent
{

    public SetAdminPasswordContent(string password, string authenticationToken) : base("SetAdminPassword")
    {
        var dict = new Dictionary<string, object?>()
        {
            {"Password", password },
            {"AuthenticationToken", authenticationToken },
        };
        Data = new FactoryServerContentData(dict);
    }
}
