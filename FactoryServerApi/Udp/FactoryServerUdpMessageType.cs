using System.Text.Json.Serialization;

namespace FactoryServerApi.Udp;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FactoryServerUdpMessageType : byte
{
    PollServerState = 0,
    ServerStateResponse = 1,
}
