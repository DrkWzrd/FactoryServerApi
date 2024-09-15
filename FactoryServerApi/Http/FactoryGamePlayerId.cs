using System.Buffers.Binary;

namespace FactoryServerApi.Http;

public readonly struct FactoryGamePlayerId
{

    private readonly ReadOnlyMemory<byte> _steamId;
    private readonly string? _epicId;

    private readonly byte _platformByte;

    public bool IsSteamId => _platformByte == 6;
    public bool IsEpicId => _platformByte == 1;

    public FactoryGamePlayerId(long steamId64)
    {
        var byteSpan = new byte[8];
        BinaryPrimitives.WriteInt64BigEndian(byteSpan, steamId64);
        _steamId = byteSpan;
        _platformByte = 6;
    }

    public FactoryGamePlayerId(string epicIdHexString)
    {
        _epicId = epicIdHexString.StartsWith("0x") ? epicIdHexString[2..] : epicIdHexString;
        _platformByte = 1;
    }

    public override string ToString()
    {
        return _platformByte == 1
            ? $"{_platformByte:X}{_epicId}"
            : $"{_platformByte:X}{Convert.ToHexString(_steamId.Span)}";
    }

}
