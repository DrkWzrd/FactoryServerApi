using System.Buffers.Binary;
using System.Text;

namespace FactoryServerApi.Udp;

//public class FactoryServerSubStates : IReadOnlyList<FactoryServerSubState>, IReadOnlyDictionary<FactoryServerSubStateId, FactoryServerSubState>
//{

//    private readonly SortedList _dictionary;

//    public FactoryServerSubState this[int index] => (FactoryServerSubState)_dictionary.GetByIndex(index)!;

//    public FactoryServerSubState this[FactoryServerSubStateId key] => (FactoryServerSubState)_dictionary[key]!;

//    public int Count => _dictionary.Count;

//    public IEnumerable<FactoryServerSubStateId> Keys => _dictionary.Keys.Cast<FactoryServerSubStateId>();

//    public IEnumerable<FactoryServerSubState> Values => _dictionary.Values.Cast<FactoryServerSubState>();

//    internal FactoryServerSubStates(int count)
//    {
//        _dictionary = new SortedList(count);
//    }

//    internal void Add(FactoryServerSubState subState)
//    {
//        _dictionary.Add(subState.SubStateId, subState);
//    }

//    public bool ContainsKey(FactoryServerSubStateId key)
//    {
//        return _dictionary.ContainsKey(key);
//    }

//    public IEnumerator<FactoryServerSubState> GetEnumerator()
//    {
//        return Values.GetEnumerator();
//    }

//    public bool TryGetValue(FactoryServerSubStateId key, [MaybeNullWhen(false)] out FactoryServerSubState value)
//    {
//        if (_dictionary.ContainsKey(key))
//        {
//            value = (FactoryServerSubState)_dictionary[key]!;
//            return true;
//        }
//        value = default;
//        return false;
//    }

//    IEnumerator IEnumerable.GetEnumerator()
//    {
//        return _dictionary.GetEnumerator();
//    }

//    IEnumerator<KeyValuePair<FactoryServerSubStateId, FactoryServerSubState>> IEnumerable<KeyValuePair<FactoryServerSubStateId, FactoryServerSubState>>.GetEnumerator()
//    {
//        return _dictionary.Cast<KeyValuePair<FactoryServerSubStateId, FactoryServerSubState>>().GetEnumerator();
//    }
//}

public class FactoryServerStateResponse
{
    private readonly List<FactoryServerSubState> _subStates;

    public ulong Cookie { get; private init; }
    public FactoryServerState ServerState { get; private init; }
    public uint ServerNetCL { get; private init; }
    public FactoryServerFlags ServerFlags { get; private init; }
    public IReadOnlyList<FactoryServerSubState> SubStates => _subStates;
    public string ServerName { get; private set; }

    public DateTimeOffset ReceivedUtc { get; }

    private FactoryServerStateResponse(byte numSubStates, DateTimeOffset receivedUtc)
    {
        _subStates = new(numSubStates);
        ServerName = string.Empty;
        ReceivedUtc = receivedUtc;
    }

    public static FactoryServerStateResponse Parse(ReadOnlySpan<byte> data, DateTimeOffset receivedUtc)
    {
        var numSubStates = data[21];
        var response = new FactoryServerStateResponse(numSubStates, receivedUtc)
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
