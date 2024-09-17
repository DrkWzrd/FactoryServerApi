namespace FactoryServerApi.Http.Requests.Contents;

internal class PasswordlessLoginContent : FactoryServerContent
{
    public PasswordlessLoginContent(FactoryServerPrivilegeLevel minimumPrivilegeLevel) : base("PasswordlessLogin")
    {
        Data = new FactoryServerContentData("MinimumPrivilegeLevel", minimumPrivilegeLevel.ToString());
    }
}
