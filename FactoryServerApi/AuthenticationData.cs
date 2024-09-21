using FactoryServerApi.Http.Responses;
using System.Text;
using System.Text.Json.Nodes;

namespace FactoryServerApi;

public record AuthenticationData
{
    public static readonly AuthenticationData Empty = new();

    public string? AuthenticationToken { get; }

    public FactoryServerPrivilegeLevel TokenPrivilegeLevel { get; }

    private AuthenticationData(string? authenticationToken, FactoryServerPrivilegeLevel tokenPrivilegeLevel)
    {
        AuthenticationToken = authenticationToken;
        TokenPrivilegeLevel = tokenPrivilegeLevel;
    }

    private AuthenticationData() : this(null, FactoryServerPrivilegeLevel.NotAuthenticated)
    {
    }

    public AuthenticationData(string authenticationToken) : this(authenticationToken, GetTokenLevel(authenticationToken))
    {
    }

    public AuthenticationData(LoginData data) : this(data.AuthenticationToken, GetTokenLevel(data.AuthenticationToken))
    {
    }

    internal static FactoryServerPrivilegeLevel GetTokenLevel(string authenticationToken)
    {
        var grantsBase64 = authenticationToken.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0];
        var grantsBytes = Convert.FromBase64String(grantsBase64);
        var grantsJson = Encoding.UTF8.GetString(grantsBytes);

        var jObj = JsonNode.Parse(grantsJson) ?? throw new InvalidDataException();

        var pl = jObj["pl"] ?? throw new InvalidDataException();

        var value = pl.GetValue<string>();

        return Enum.Parse<FactoryServerPrivilegeLevel>(value);
    }
}
