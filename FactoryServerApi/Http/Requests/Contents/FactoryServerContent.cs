using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace FactoryServerApi.Http.Requests.Contents;

public abstract class FactoryServerContent : HttpContent
{

    internal static readonly JsonSerializerOptions FactoryServerJsonOptions = new(JsonSerializerDefaults.Web)
    {
#if DEBUG
        //WriteIndented = true,
#endif
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        IncludeFields = true,
    };

    public string Function { get; }

    protected IFactoryServerContentData? Data { get; init; }

    private string? _computedJson;
    private long _computedLength;

    protected FactoryServerContent(string function)
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
        //Headers.ContentLength = _computedLength;
    }

    private string GetJson()
    {
        var content = new Dictionary<string, object>
        {
            { "function", Function }
        };
        if (Data is not null)
            content.Add("data", Data.GetJson());

        var json = JsonSerializer.Serialize(content, FactoryServerJsonOptions);
        _computedLength = Encoding.UTF8.GetByteCount(json);
        return json;
    }

    protected override bool TryComputeLength(out long length)
    {
        //length = 0;
        //return false;
        _computedJson ??= GetJson();

        length = _computedLength;
        return true;
    }
}
