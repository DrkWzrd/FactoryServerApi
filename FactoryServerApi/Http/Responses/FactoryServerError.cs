using System.Text.Json.Serialization;

namespace FactoryServerApi.Http.Responses;

public class FactoryServerError
{
    [JsonPropertyName("errorCode")]
    public string ErrorCode { get; init; } = string.Empty;

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; init; }

    [JsonPropertyName("errorData")]
    public object? ErrorData { get; init; }
}