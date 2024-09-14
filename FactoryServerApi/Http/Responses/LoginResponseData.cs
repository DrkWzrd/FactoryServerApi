namespace FactoryServerApi.Http.Responses;

public class LoginResponseData
{
    public string AuthenticationToken { get; }

    internal LoginResponseData(string authenticationToken)
    {
        AuthenticationToken = authenticationToken;
    }
}
