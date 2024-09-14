using System.Text.Json.Serialization;

namespace FactoryServerApi.Http;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HealthState
{
    Healthy,
    Slow,
}
