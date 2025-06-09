namespace FactoryServerApi;

public class UdpOptions
{

    public TimeSpan DelayBetweenPolls { get; init; }

    public int MessagesPerPoll { get; init; }

    public int TimeoutRetriesBeforeStop { get; init; }

    public ushort ProtocolMagic { get; init; }

    public byte ProtocolVersion { get; init; }

    public byte MessageTermination { get; init; }
}
