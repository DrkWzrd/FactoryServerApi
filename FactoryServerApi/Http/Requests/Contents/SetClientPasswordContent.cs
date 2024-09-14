namespace FactoryServerApi.Http.Requests.Contents;

internal class SetClientPasswordContent : FactoryServerContent
{

    public SetClientPasswordContent(string password) : base("SetClientPassword")
    {
        Data = new SinglePropertyFactoryServerContentData("Password", password);
    }
}
