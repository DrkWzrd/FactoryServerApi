
namespace FactoryServerApiNew;

public class UdpOptions
{

    public TimeSpan DelayBetweenPolls { get; set; } = TimeSpan.FromSeconds(10);
    public int MessagesPerPoll { get; set; } = 2;
    public int TimeoutRetriesBeforeStop { get; set; } = 3;
    public ushort ProtocolMagic { get; set; } = 0xF6D5;
    public byte ProtocolVersion { get; set; } = 1;
    public byte MessageTermination { get; set; } = 1;
}
