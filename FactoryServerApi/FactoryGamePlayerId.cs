using System.Buffers.Binary;

namespace FactoryServerApi;

public readonly struct FactoryGamePlayerId
{

    private readonly string? _steamId;
    private readonly string? _epicId;

    private readonly byte _platformByte;

    public bool IsSteamId => _platformByte == 6;
    public bool IsEpicId => _platformByte == 1;

    public FactoryGamePlayerId(long steamId64)
    {
        Memory<byte> bytes = new byte[8];
        BinaryPrimitives.WriteInt64BigEndian(bytes.Span, steamId64);
        _steamId = Convert.ToHexString(bytes.Span);
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
            : $"{_platformByte:X}{_steamId}";
    }

}
