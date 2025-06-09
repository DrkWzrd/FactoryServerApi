namespace FactoryServerApi.Http.Requests.Contents;

internal class SetAdminPasswordRequestContent : FactoryServerRequestContent
{

    public SetAdminPasswordRequestContent(ReadOnlyMemory<char> password, string authenticationToken) : base("SetAdminPassword")
    {
        if (password.IsEmpty)
            throw new InvalidOperationException();

        Dictionary<string, object> dict = new()
        {
            {"Password", password },
            {"AuthenticationToken", authenticationToken },
        };
        Data = new FactoryServerRequestContentData(dict);
    }
}
