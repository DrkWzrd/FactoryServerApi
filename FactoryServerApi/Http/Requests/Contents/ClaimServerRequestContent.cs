namespace FactoryServerApi.Http.Requests.Contents;

internal class ClaimServerRequestContent : FactoryServerRequestContent
{

    public ClaimServerRequestContent(string serverName, ReadOnlySpan<char> adminPassword) : base("ClaimServer")
    {
        if (adminPassword.IsEmpty)
            throw new InvalidOperationException();

        var dict = new Dictionary<string, object?>()
        {
            {"ServerName", serverName },
            {"AdminPassword", adminPassword.ToString() },
        };
        Data = new FactoryServerRequestContentData(dict);
    }

}
