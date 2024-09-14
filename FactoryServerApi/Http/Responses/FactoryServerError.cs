using System.Text.Json.Serialization;

namespace FactoryServerApi.Http.Responses;

//var response = await _httpClient.PostAsync("your-api-endpoint/HealthCheck", jsonContent);

//// Ensure the request was successful
//response.EnsureSuccessStatusCode();

//    // Read and deserialize the response content
//    var responseContent = await response.Content.ReadAsStringAsync();
//var healthCheckResponse = JsonSerializer.Deserialize<HealthCheckResponse>(responseContent);

//    return healthCheckResponse;

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
