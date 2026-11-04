namespace FactoryServerApi.Http.Requests.Contents;

internal class SetClientPasswordRequestContent : FactoryServerRequestContent
{

    public SetClientPasswordRequestContent(ReadOnlyMemory<char>? password) : base("SetClientPassword")
    {
        // Serialize as a string; a raw ReadOnlyMemory<char> would be encoded as a char array.
        Data = new FactoryServerRequestContentData("Password", password?.ToString());
    }
}
