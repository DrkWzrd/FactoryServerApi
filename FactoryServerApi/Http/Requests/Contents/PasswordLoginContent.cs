namespace FactoryServerApi.Http.Requests.Contents;

internal class PasswordLoginContent : FactoryServerContent
{
    public PasswordLoginContent(FactoryServerPrivilegeLevel minimumPrivilegeLevel, string password) : base("PasswordLogin")
    {
        var dict = new Dictionary<string, object?>()
        {
            {"MinimumPrivilegeLevel", minimumPrivilegeLevel },
            {"Password", password }
        };
        Data = new DictionaryFactoryServerContentData(dict);
    }
}
