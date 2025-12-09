using System.Text.Json.Serialization;

namespace FactoryServerApi;

[JsonConverter(typeof(JsonStringEnumConverter<FactoryServerHealthState>))]
public enum FactoryServerHealthState
{
    Healthy,
    Slow,
}
