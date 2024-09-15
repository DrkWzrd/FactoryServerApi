using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace FactoryServerApi.Http.Responses;

public class LoginResponseData
{
    public string AuthenticationToken { get; }

    public FactoryServerPrivilegeLevel LevelGranted { get; }

    [JsonConstructor]
    internal LoginResponseData(string authenticationToken)
    {
        AuthenticationToken = authenticationToken;

        var grantsBase64 = authenticationToken.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0];
        var grantsBytes = Convert.FromBase64String(grantsBase64);
        var grantsJson = Encoding.UTF8.GetString(grantsBytes);

        var jObj = JsonNode.Parse(grantsJson) ?? throw new InvalidDataException();

        var pl = jObj["pl"] ?? throw new InvalidDataException();

        LevelGranted = Enum.Parse<FactoryServerPrivilegeLevel>(pl.GetValue<string>());
    }
}
