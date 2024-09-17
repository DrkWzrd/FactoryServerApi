using System.Text.Json.Serialization;

namespace FactoryServerApi.Http.Responses;

public class HealthCheckResponseData
{
    public FactoryServerHealthState Health { get; }
    public string? ServerCustomData { get; }

    [JsonConstructor]
    internal HealthCheckResponseData(FactoryServerHealthState health, string? serverCustomData)
    {
        Health = health;
        ServerCustomData = serverCustomData;
    }


}
