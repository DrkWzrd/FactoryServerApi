namespace FactoryServerApi.Http.Requests.Contents;

internal class ClaimServerRequestContent : FactoryServerRequestContent
{

    public ClaimServerRequestContent(string serverName, ReadOnlyMemory<char> adminPassword) : base("ClaimServer")
    {
        if (adminPassword.IsEmpty)
            throw new InvalidOperationException();

        Dictionary<string, object> dict = new()
        {
            {"ServerName", serverName },
            {"AdminPassword", adminPassword.ToString() },
        };
        Data = new FactoryServerRequestContentData(dict);
    }

}
