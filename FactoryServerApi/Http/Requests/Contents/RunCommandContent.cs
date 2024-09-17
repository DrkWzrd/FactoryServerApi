namespace FactoryServerApi.Http.Requests.Contents;

internal class RunCommandContent : FactoryServerContent
{
    public RunCommandContent(string command) : base("RunCommand")
    {
        Data = new FactoryServerContentData("Command", command);
    }
}
