namespace FactoryServerApi.Http.Responses;

public class RunCommandResponseData
{
    public string CommandResult { get; }

    internal RunCommandResponseData(string commandResult)
    {
        CommandResult = commandResult;
    }
}
