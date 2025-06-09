using System.Buffers;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;

namespace FactoryServerApi.Http.Requests.Contents;

public abstract class FactoryServerRequestContent : HttpContent
{
    public string Function { get; }

    private protected FactoryServerRequestContentData? Data { get; init; }

    private byte[]? _buffer;
    private int _length;

    protected FactoryServerRequestContent(string function)
    {
        Function = function;
        Headers.ContentEncoding.Clear();
        Headers.ContentType = MediaTypeHeaderValue.Parse(MediaTypeNames.Application.Json);
    }

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        EnsureJsonEncoded();
        await stream.WriteAsync(_buffer.AsMemory(0, _length));
    }

    protected override bool TryComputeLength(out long length)
    {
        EnsureJsonEncoded();
        length = _length;
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (_buffer is not null)
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = null!;
        }
    }

    private void EnsureJsonEncoded()
    {
        if (_buffer is not null)
            return;

        var writer = new ArrayBufferWriter<byte>();
        using var jsonWriter = new Utf8JsonWriter(writer, new JsonWriterOptions { SkipValidation = true });

        jsonWriter.WriteStartObject();
        jsonWriter.WriteString("function", Function);

        if (Data is not null)
        {
            jsonWriter.WritePropertyName("data");
            JsonSerializer.Serialize(jsonWriter, Data);
        }

        jsonWriter.WriteEndObject();
        jsonWriter.Flush();

        _length = writer.WrittenCount;

        // Rent and copy to pooled buffer
        _buffer = ArrayPool<byte>.Shared.Rent(_length);
        writer.WrittenSpan.CopyTo(_buffer);
    }
}