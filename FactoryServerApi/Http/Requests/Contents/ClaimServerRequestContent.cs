namespace FactoryServerApi.Http.Requests.Contents;

internal class ClaimServerRequestContent : FactoryServerRequestContent
{

    public ClaimServerRequestContent(string serverName, ReadOnlyMemory<char> adminPassword) : base("ClaimServer")
    {
        if (adminPassword.IsEmpty)
            throw new InvalidOperationException();

        var dict = new Dictionary<string, object?>()
        {
            {"ServerName", serverName },
            {"AdminPassword", adminPassword },
        };
        Data = new FactoryServerRequestContentData(dict);
    }

}
