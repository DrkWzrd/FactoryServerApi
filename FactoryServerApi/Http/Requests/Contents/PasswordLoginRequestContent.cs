namespace FactoryServerApi.Http.Requests.Contents;

internal class PasswordLoginRequestContent : FactoryServerRequestContent
{
    public PasswordLoginRequestContent(FactoryServerPrivilegeLevel minimumPrivilegeLevel, ReadOnlyMemory<char>? password) : base("PasswordLogin")
    {
        if ((password is null || password.Value.IsEmpty) && minimumPrivilegeLevel > FactoryServerPrivilegeLevel.Client)
            throw new InvalidOperationException();

        var dict = new Dictionary<string, object?>()
        {
            {"MinimumPrivilegeLevel", minimumPrivilegeLevel },
            {"Password", password }
        };
        Data = new FactoryServerRequestContentData(dict);
    }
}
