using System.Text.Json.Serialization;

namespace FactoryServerApi.Http.Responses;

public class FactoryServerResponseContent<T> where T : FactoryServerResponseContentData
{
    public T? Data { get; init; }

    [JsonInclude]
    private string? ErrorCode { get; init; }

    [JsonInclude]
    private string? ErrorMessage { get; init; }

    [JsonInclude]
    private object? ErrorData { get; init; }

    [JsonIgnore]
    public FactoryServerError? Error => string.IsNullOrEmpty(ErrorCode)
        ? null
        : new FactoryServerError
        {
            ErrorCode = ErrorCode!,
            ErrorMessage = ErrorMessage,
            ErrorData = ErrorData
        };
}
