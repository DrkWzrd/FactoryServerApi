using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace FactoryServerApi.Http.Requests.Contents;

public abstract class FactoryServerRequestContent : HttpContent
{
    public string Function { get; }

    private protected FactoryServerRequestContentData? Data { get; init; }

    private string? _computedJson;
    private long _computedLength;

    protected FactoryServerRequestContent(string function)
    {
        Function = function;
        Headers.ContentEncoding.Clear();
        Headers.ContentType = MediaTypeHeaderValue.Parse(MediaTypeNames.Application.Json);
    }

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        using var sWriter = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);

        _computedJson ??= GetJson();

        await sWriter.WriteAsync(_computedJson);
    }

    private string GetJson()
    {
        var content = new Dictionary<string, object>
        {
            { "function", Function }
        };
        if (Data is not null)
            content.Add("data", Data);

        var json = JsonSerializer.Serialize(content);
        _computedLength = Encoding.UTF8.GetByteCount(json);
        return json;
    }

    protected override bool TryComputeLength(out long length)
    {
        _computedJson ??= GetJson();

        length = _computedLength;
        return true;
    }
}
