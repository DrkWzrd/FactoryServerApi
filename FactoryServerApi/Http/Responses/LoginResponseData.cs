using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace FactoryServerApi.Http.Responses;

public class LoginResponseData
{

    private FactoryServerPrivilegeLevel? _computedLevelGranted;

    public string AuthenticationToken { get; }

    public FactoryServerPrivilegeLevel GetLevelGrantedAsync()
    {
        if (_computedLevelGranted is null)
        {
            var grantsBase64 = AuthenticationToken.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0];
            var grantsBytes = Convert.FromBase64String(grantsBase64);
            var grantsJson = Encoding.UTF8.GetString(grantsBytes);

            var jObj = JsonNode.Parse(grantsJson) ?? throw new InvalidDataException();

            var pl = jObj["pl"] ?? throw new InvalidDataException();

            _computedLevelGranted = Enum.Parse<FactoryServerPrivilegeLevel>(pl.GetValue<string>());
        }
        return _computedLevelGranted.Value;
    }

    [JsonConstructor]
    internal LoginResponseData(string authenticationToken)
    {
        AuthenticationToken = authenticationToken;


    }
}
