namespace FactoryServerApi.Http.Requests.Contents;

internal class ApplyServerOptionsContent : FactoryServerContent
{
    public ApplyServerOptionsContent(IDictionary<string, string> updatedServerOptions) : base("ApplyServerOptions")
    {
        Data = new FactoryServerContentData("UpdatedServerOptions", updatedServerOptions);
    }
}
