namespace FactoryServerApi.Http.Responses;

public class RunCommandData : FactoryServerResponseContentData
{
    public string CommandResult { get; }

    public RunCommandData(string commandResult)
    {
        CommandResult = commandResult;
    }
}
