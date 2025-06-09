namespace FactoryServerApi.Http.Requests.Contents;

internal class ApplyServerOptionsRequestContent : FactoryServerRequestContent
{
    public ApplyServerOptionsRequestContent(IDictionary<string, string> updatedServerOptions) : base("ApplyServerOptions")
    {
        Data = new FactoryServerRequestContentData("UpdatedServerOptions", updatedServerOptions);
    }
}
