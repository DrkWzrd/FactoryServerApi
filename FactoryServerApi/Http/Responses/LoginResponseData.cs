using System.Text.Json.Serialization;

namespace FactoryServerApi.Http.Responses;

public class LoginResponseData
{
    public string AuthenticationToken { get; }

    [JsonConstructor]
    internal LoginResponseData(string authenticationToken)
    {
        AuthenticationToken = authenticationToken;
    }
}
