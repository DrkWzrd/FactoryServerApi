using System.Buffers;
using System.Text.Json;

namespace FactoryServerApi;

public static class AuthenticationTokenHelper
{
    public static FactoryServerPrivilegeLevel GetTokenLevel(ReadOnlyMemory<char>? authenticationToken)
    {
        if (authenticationToken is null)
            return FactoryServerPrivilegeLevel.NotAuthenticated;

        var splitPoint = authenticationToken.Value.Span.IndexOf('.');

        var tokenPayloadBase64 = authenticationToken.Value.Span[..splitPoint];

        Span<byte> tokenPayloadBytes = ArrayPool<byte>.Shared.Rent(tokenPayloadBase64.Length * 2);

        if (!Convert.TryFromBase64Chars(tokenPayloadBase64, tokenPayloadBytes, out var bytesLength))
            throw new InvalidDataException("Token payload was invalid.");

        tokenPayloadBytes = tokenPayloadBytes[..bytesLength];

        var payloadReader = new Utf8JsonReader(tokenPayloadBytes);

        var payload = JsonSerializer.Deserialize<AuthenticationTokenPayload>(ref payloadReader);

        return payload.PL;
    }

    private readonly struct AuthenticationTokenPayload
    {
        public FactoryServerPrivilegeLevel PL { get; }
    }
}
