using System.Buffers;
using System.Text.Json;

namespace FactoryServerApi;

public static class AuthenticationTokenHelper
{
    public static FactoryServerPrivilegeLevel GetTokenLevel(ReadOnlyMemory<char>? authenticationToken)
    {
        if (authenticationToken is null)
            return FactoryServerPrivilegeLevel.NotAuthenticated;

        int splitPoint = authenticationToken.Value.Span.IndexOf('.');

        ReadOnlySpan<char> tokenPayloadBase64 = authenticationToken.Value.Span[..splitPoint];

        byte[] tokenPayloadBytesRaw = ArrayPool<byte>.Shared.Rent(tokenPayloadBase64.Length * 2);

        Span<byte> tokenPayloadBytes = tokenPayloadBytesRaw.AsSpan(0, tokenPayloadBase64.Length * 2);

        try
        {

            if (!Convert.TryFromBase64Chars(tokenPayloadBase64, tokenPayloadBytes, out int bytesLength))
                throw new InvalidDataException("Token payload was invalid.");

            tokenPayloadBytes = tokenPayloadBytes[..bytesLength];

            Utf8JsonReader payloadReader = new(tokenPayloadBytes);

            AuthenticationTokenPayload payload = JsonSerializer.Deserialize<AuthenticationTokenPayload>(ref payloadReader);

            return payload.PL;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(tokenPayloadBytesRaw);
        }

    }

    private readonly struct AuthenticationTokenPayload
    {
        public FactoryServerPrivilegeLevel PL { get; }
    }
}
