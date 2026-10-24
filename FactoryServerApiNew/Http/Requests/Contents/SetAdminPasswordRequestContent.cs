namespace FactoryServerApiNew.Http.Requests.Contents;

internal class SetAdminPasswordRequestContent : FactoryServerRequestContent
{

    public SetAdminPasswordRequestContent(ReadOnlyMemory<char> password, string authenticationToken) : base("SetAdminPassword")
    {
        if (password.IsEmpty)
            throw new InvalidOperationException();

        Dictionary<string, object> dict = new()
        {
            // Serialize as a string; a raw ReadOnlyMemory<char> would be encoded as a char array.
            {"Password", password.ToString() },
            {"AuthenticationToken", authenticationToken },
        };
        Data = new FactoryServerRequestContentData(dict);
    }
}
