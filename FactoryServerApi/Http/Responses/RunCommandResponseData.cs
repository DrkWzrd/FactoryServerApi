using System.Text.Json.Serialization;

namespace FactoryServerApi.Http.Responses;

public class RunCommandResponseData
{
    public string CommandResult { get; }

    [JsonConstructor]
    internal RunCommandResponseData(string commandResult)
    {
        CommandResult = commandResult;
    }
}
