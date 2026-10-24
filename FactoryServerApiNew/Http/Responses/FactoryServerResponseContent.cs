using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace FactoryServerApiNew.Http.Responses;

public class FactoryServerResponseContent<T> where T : FactoryServerResponseContentData
{
    internal FactoryServerResponseContent(T? data = default, FactoryServerError? error = null)
    {
        Data = data;
        ErrorCode = error?.ErrorCode;
        ErrorMessage = error?.ErrorMessage;
        ErrorData = error?.ErrorData;
    }

    public T? Data { get; init; }

    [JsonIgnore]
    [MemberNotNullWhen(true, nameof(Data))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

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
