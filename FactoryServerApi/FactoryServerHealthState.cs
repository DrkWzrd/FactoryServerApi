using System.Text.Json.Serialization;

namespace FactoryServerApi;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FactoryServerHealthState
{
    Healthy,
    Slow,
}
