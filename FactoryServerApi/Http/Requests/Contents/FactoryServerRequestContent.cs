using System.Buffers;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace FactoryServerApi.Http.Requests.Contents;

public abstract class FactoryServerRequestContent : HttpContent
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private ArrayBufferWriter<byte>? _writer;
    private bool _disposed;

    public string Function { get; }

    private protected FactoryServerRequestContentData? Data { get; init; }

    protected FactoryServerRequestContent(string function)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(function);

        Function = function;
        Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json) { CharSet = "utf-8" };
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        => SerializeToStreamAsync(stream, context, CancellationToken.None);

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        => stream.WriteAsync(GetPayload(), cancellationToken).AsTask();

    protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        => stream.Write(GetPayload().Span);

    protected override Task<Stream> CreateContentReadStreamAsync()
    {
        ReadOnlyMemory<byte> payload = GetPayload();

        // Wrap the existing buffer instead of copying it
        Stream stream = MemoryMarshal.TryGetArray(payload, out ArraySegment<byte> segment)
            ? new MemoryStream(segment.Array ?? throw new NullReferenceException(), segment.Offset, segment.Count, writable: false)
            : new MemoryStream(payload.ToArray(), writable: false);

        return Task.FromResult(stream);
    }

    protected override bool TryComputeLength(out long length)
    {
        if (_disposed)
        {
            length = 0;
            return false;
        }

        length = GetPayload().Length;
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _disposed = true;
            _writer = null;
        }

        base.Dispose(disposing);
    }

    private ReadOnlyMemory<byte> GetPayload()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return (_writer ??= Encode()).WrittenMemory;
    }

    private ArrayBufferWriter<byte> Encode()
    {
        ArrayBufferWriter<byte> writer = new(256);

        using (Utf8JsonWriter json = new(writer))
        {
            json.WriteStartObject();
            json.WriteString("function", Function);

            if (Data is not null)
            {
                json.WritePropertyName("data");
                // Use the runtime type so derived properties aren't dropped
                JsonSerializer.Serialize(json, Data, Data.GetType(), SerializerOptions);
            }

            json.WriteEndObject();
        } // Dispose flushes

        return writer;
    }
}