using System.Text.Json.Serialization;

namespace FactoryServerApiNew.Udp;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FactoryServerUdpMessageType : byte
{
    PollServerState = 0,
    ServerStateResponse = 1,
}
