namespace FactoryServerApi.Http.Requests.Contents;

internal class HealthCheckRequestContent : FactoryServerRequestContent
{

    public HealthCheckRequestContent(string? clientCustomData) : base("HealthCheck")
    {
        Data = new FactoryServerRequestContentData("ClientCustomData", clientCustomData);
    }

}
