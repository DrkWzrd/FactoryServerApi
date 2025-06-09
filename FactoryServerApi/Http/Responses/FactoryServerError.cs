using System.Text.Json.Serialization;

namespace FactoryServerApi.Http.Responses;

//public class FactoryServerResponse<T> where T : class
//{

//    private static readonly FrozenDictionary<int, string> _statusCodesMeanings = new Dictionary<int, string>
//    {
//        { 200, "The function has been executed successfully." },
//        { 201, "The function has been executed and returned no error. New file has been created." },
//        { 202, "The function has been executed, but is still being processed." },
//        { 204, "The function has been executed successfully, but returned no data nor error." },
//        { 400, "Request body failed to be parsed as valid JSON or multipart request." },
//        { 401, "Authentication token is missing, cannot be parsed, or has expired." },
//        { 403, "Provided authentication does not allow executing the provided function, or a function requiring authentication have been called without one" },
//        { 404, "The specified function cannot be found, or the function cannot find the specified resource." },
//        { 415, "Specified charset or content encoding is not supported, or multipart data is malformed." },
//        { 500, "An internal server error has occurred when executing the function." }
//    }.ToFrozenDictionary();

//    private readonly HttpStatusCode _statusCode;

//    public string? Description
//    {
//        get
//        {
//            if (_statusCodesMeanings.TryGetValue((int)_statusCode, out var meaning))
//                return meaning;
//            return null;
//        }
//    }

//    public T? Result { get; }

//    public FactoryServerError? Error { get; }

//    internal FactoryServerResponse(HttpStatusCode statusCode, T? result, FactoryServerError? error)
//    {
//        _statusCode = statusCode;
//        Result = result;
//        Error = error;
//    }

//}

public class FactoryServerError
{
    [JsonPropertyName("errorCode")]
    public string ErrorCode { get; init; } = string.Empty;

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; init; }

    [JsonPropertyName("errorData")]
    public object? ErrorData { get; init; }
}