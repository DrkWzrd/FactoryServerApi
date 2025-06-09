namespace FactoryServerApi.Http.Requests.Contents;

internal class SetClientPasswordRequestContent : FactoryServerRequestContent
{

    public SetClientPasswordRequestContent(ReadOnlyMemory<char>? password) : base("SetClientPassword")
    {
        Data = new FactoryServerRequestContentData("Password", password);
    }
}
