namespace FactoryServerApi.Http.Requests.Contents;

internal class HealthCheckContent : FactoryServerContent
{

    public HealthCheckContent(string? clientCustomData) : base("HealthCheck")
    {
        Data = new SinglePropertyFactoryServerContentData("ClientCustomData", clientCustomData);
    }

}
