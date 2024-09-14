namespace FactoryServerApi.Http.Requests.Contents;

internal class ClaimServerContent : FactoryServerContent
{

    public ClaimServerContent(string serverName, string adminPassword) : base("ClaimServer")
    {
        var dict = new Dictionary<string, object?>()
        {
            {"ServerName", serverName },
            {"AdminPassword", adminPassword },
        };
        Data = new DictionaryFactoryServerContentData(dict);
    }

}
