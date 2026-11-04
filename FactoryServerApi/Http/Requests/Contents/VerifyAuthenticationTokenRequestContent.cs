namespace FactoryServerApi.Http.Requests.Contents;

internal class VerifyAuthenticationTokenRequestContent : FactoryServerRequestContent
{
    //The docs are outdated, and we don't know the actual shape, keep it like this (we tested some options)
    public VerifyAuthenticationTokenRequestContent(string authenticationToken) :
        base("VerifyAuthenticationToken")
    {
        Dictionary<string, object> dict = new()
        {
            {"authenticationToken", authenticationToken },
        };
        Data = new FactoryServerRequestContentData(dict);

    }
}
