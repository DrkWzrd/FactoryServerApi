using System.Text.Json.Serialization;

namespace FactoryServerApi.Udp;

[JsonConverter(typeof(JsonStringEnumConverter))]
[Flags]
public enum FactoryServerFlags : ulong
{
    Modded = 1,
    Custom1 = 2,
    Custom2 = 4,
    Custom3 = 8,
    Custom4 = 16,
}
