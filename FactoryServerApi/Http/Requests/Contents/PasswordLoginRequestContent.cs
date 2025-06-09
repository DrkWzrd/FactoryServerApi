namespace FactoryServerApi.Http.Requests.Contents;

internal class PasswordLoginRequestContent : FactoryServerRequestContent
{
    public PasswordLoginRequestContent(FactoryServerPrivilegeLevel minimumPrivilegeLevel, ReadOnlyMemory<char> password) : base("PasswordLogin")
    {
        if (password.IsEmpty && minimumPrivilegeLevel > FactoryServerPrivilegeLevel.Client)
            throw new InvalidOperationException("What are you trying to do? Login as admin without the admin password?");

        var dict = new Dictionary<string, object>()
        {
            {"MinimumPrivilegeLevel", minimumPrivilegeLevel },
            {"Password", password.ToString() }
        };
        Data = new FactoryServerRequestContentData(dict);
    }
}
