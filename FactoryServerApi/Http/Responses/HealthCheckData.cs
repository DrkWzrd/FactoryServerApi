namespace FactoryServerApi.Http.Responses;

public class HealthCheckData : FactoryServerResponseContentData
{
    public FactoryServerHealthState Health { get; init; }
    public string? ServerCustomData { get; init; }

}
