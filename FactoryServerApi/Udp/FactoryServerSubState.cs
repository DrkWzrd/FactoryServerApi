﻿using System.Buffers.Binary;

namespace FactoryServerApi.Udp;

public readonly struct FactoryServerSubState
{
    public FactoryServerSubStateId SubStateId { get; private init; }
    public ushort SubStateVersion { get; private init; }

    public static FactoryServerSubState Deserialize(ReadOnlySpan<byte> data, ref int offset)
    {
        FactoryServerSubState subState = new()
        {
            SubStateId = (FactoryServerSubStateId)data[0],
            SubStateVersion = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(1, 2))
        };
        offset += 3;

        return subState;
    }
}
