namespace FactoryServerApi.Http.Responses;

public class LoginData : FactoryServerResponseContentData
{
    public string AuthenticationToken { get; }

    public LoginData(string authenticationToken)
    {
        AuthenticationToken = authenticationToken;
    }
}
