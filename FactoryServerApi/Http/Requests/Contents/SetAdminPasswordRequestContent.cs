namespace FactoryServerApi.Http.Requests.Contents;

internal class SetAdminPasswordRequestContent : FactoryServerRequestContent
{

    public SetAdminPasswordRequestContent(ReadOnlyMemory<char> password, string authenticationToken) : base("SetAdminPassword")
    {
        if (password.IsEmpty)
            throw new InvalidOperationException();

        var dict = new Dictionary<string, object?>()
        {
            {"Password", password },
            {"AuthenticationToken", authenticationToken },
        };
        Data = new FactoryServerRequestContentData(dict);
    }
}
