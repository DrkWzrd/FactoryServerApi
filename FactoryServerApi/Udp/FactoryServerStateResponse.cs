using System.Buffers.Binary;
using System.Text;

namespace FactoryServerApi.Udp;

public class FactoryServerStateResponse
{
    private readonly List<FactoryServerSubState> _subStates;

    public ulong Cookie { get; private init; }
    public FactoryServerState ServerState { get; private init; }
    public uint ServerNetCL { get; private init; }
    public FactoryServerFlags ServerFlags { get; private init; }
    public IReadOnlyList<FactoryServerSubState> SubStates => _subStates;
    public string ServerName { get; private set; }

    private FactoryServerStateResponse(byte numSubStates)
    {
        _subStates = new(numSubStates);
        ServerName = string.Empty;
    }

    public static FactoryServerStateResponse Parse(ReadOnlySpan<byte> data)
    {
        var numSubStates = data[21];
        var response = new FactoryServerStateResponse(numSubStates)
        {
            Cookie = BinaryPrimitives.ReadUInt64LittleEndian(data[..8]),
            ServerState = (FactoryServerState)data[8],
            ServerNetCL = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(9, 4)),
            ServerFlags = (FactoryServerFlags)BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(13, 8)),
        };
        int offset = 22;
        for (int i = 0; i < numSubStates; i++)
            response._subStates.Add(FactoryServerSubState.Parse(data[offset..], ref offset));

        ushort serverNameLength = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(offset, 2));
        if (serverNameLength != 0)
        {
            offset += 2;
            response.ServerName = Encoding.UTF8.GetString(data.Slice(offset, serverNameLength));
        }

        return response;
    }
}
