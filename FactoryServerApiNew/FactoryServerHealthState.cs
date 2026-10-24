using System.Text.Json.Serialization;

namespace FactoryServerApiNew;

[JsonConverter(typeof(JsonStringEnumConverter<FactoryServerHealthState>))]
public enum FactoryServerHealthState
{
    Healthy,
    Slow,
}
