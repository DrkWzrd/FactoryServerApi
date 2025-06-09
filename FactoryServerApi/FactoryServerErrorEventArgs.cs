using System.Text.Json;
using FactoryServerApi.Http.Responses;

namespace FactoryServerApi;

public class FactoryServerErrorEventArgs : EventArgs
{

    public FactoryServerError? Error { get; }

    public Exception? Exception { get; }

    internal FactoryServerErrorEventArgs(Exception ex)
    {
        Exception = ex;
    }

    internal FactoryServerErrorEventArgs(FactoryServerError error)
    {
        Error = error;
    }

    public override string ToString()
    {
        return Error is not null
            ? JsonSerializer.Serialize(Error)
            : JsonSerializer.Serialize(Exception);
    }

}
