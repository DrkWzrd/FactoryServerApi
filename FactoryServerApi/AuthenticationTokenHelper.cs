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
        if (splitPoint <= 0)
            return FactoryServerPrivilegeLevel.NotAuthenticated;

        ReadOnlySpan<char> tokenPayloadBase64 = authenticationToken.Value.Span[..splitPoint];

        var maxLength = (tokenPayloadBase64.Length * 3 + 3) / 4;

        byte[] tokenPayloadBytesRaw = ArrayPool<byte>.Shared.Rent(maxLength);

        Span<byte> tokenPayloadBytes = tokenPayloadBytesRaw.AsSpan(0, maxLength);

        try
        {
            if (!Convert.TryFromBase64Chars(tokenPayloadBase64, tokenPayloadBytes, out int bytesLength))
                return FactoryServerPrivilegeLevel.NotAuthenticated;

            tokenPayloadBytes = tokenPayloadBytes[..bytesLength];

            Utf8JsonReader payloadReader = new(tokenPayloadBytes);

            AuthenticationTokenPayload payload = JsonSerializer.Deserialize<AuthenticationTokenPayload>(ref payloadReader);

            return payload.PL;
        }
        catch (Exception)
        {
            // A property getter must not throw: a malformed token is simply "not authenticated".
            return FactoryServerPrivilegeLevel.NotAuthenticated;
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
