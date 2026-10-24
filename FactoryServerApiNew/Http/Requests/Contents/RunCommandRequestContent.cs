namespace FactoryServerApiNew.Http.Requests.Contents;

internal class RunCommandRequestContent : FactoryServerRequestContent
{
    public RunCommandRequestContent(string command) : base("RunCommand")
    {
        Data = new FactoryServerRequestContentData("Command", command);
    }
}
