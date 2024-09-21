namespace FactoryServerApi.Http.Requests.Contents;

internal class PasswordlessLoginRequestContent : FactoryServerRequestContent
{
    public PasswordlessLoginRequestContent(FactoryServerPrivilegeLevel minimumPrivilegeLevel) : base("PasswordlessLogin")
    {
        Data = new FactoryServerRequestContentData("MinimumPrivilegeLevel", minimumPrivilegeLevel);
    }
}
