using System.Text.Json.Serialization;

namespace FactoryServerApi.Http.Responses;

public class FactoryServerError
{
    public string ErrorCode { get; }
    public string? ErrorMessage { get; }
    public object? ErrorData { get; }

    [JsonConstructor]
    internal FactoryServerError(string errorCode, string? errorMessage, object? errorData)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ErrorData = errorData;
    }
}
