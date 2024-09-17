namespace FactoryServerApi.Http.Requests.Contents;

internal class SetClientPasswordContent : FactoryServerContent
{

    public SetClientPasswordContent(string password) : base("SetClientPassword")
    {
        Data = new FactoryServerContentData("Password", password);
    }
}
