using System.Buffers.Binary;

namespace FactoryServerApi.Udp;

public readonly struct FactoryServerSubState
{
    public FactoryServerSubStateId SubStateId { get; private init; }
    public ushort SubStateVersion { get; private init; }

    public static FactoryServerSubState Parse(ReadOnlySpan<byte> data, ref int offset)
    {
        var subState = new FactoryServerSubState
        {
            SubStateId = (FactoryServerSubStateId)data[0],
            SubStateVersion = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(1, 2))
        };
        offset += 3;

        return subState;
    }
}
