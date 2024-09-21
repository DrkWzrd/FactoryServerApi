using FactoryServerApi.Http.Responses;
using System.Text.Json;

namespace FactoryServerApi;

public class FactoryServerErrorEventArgs : EventArgs
{

    public FactoryServerError? Error { get; }

    public Exception? Exception { get; }


    public FactoryServerErrorEventArgs(Exception ex)
    {
        Exception = ex;
    }

    public FactoryServerErrorEventArgs(FactoryServerError error)
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
