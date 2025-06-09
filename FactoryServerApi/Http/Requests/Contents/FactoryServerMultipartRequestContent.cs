using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FactoryServerApi.Http.Requests.Contents;

public abstract class FactoryServerMultipartRequestContent : MultipartFormDataContent
{
    private static readonly MediaTypeHeaderValue JsonMediaType = new(MediaTypeNames.Application.Json);
    private static readonly MediaTypeHeaderValue PlainTextMediaType = new(MediaTypeNames.Text.Plain);
    private static readonly MediaTypeHeaderValue OctetStreamMediaType = new(MediaTypeNames.Application.Octet);

    public string Function { get; }

    private protected FactoryServerMultipartRequestContent(string function, FactoryServerRequestContentData data, string partName, string fileName, object part)
    {
        Function = function;
        Headers.ContentEncoding.Clear();

        // Build and serialize metadata part
        var requestData = new
        {
            function = Function,
            data
        };

        var requestDataJson = JsonSerializer.Serialize(requestData);
        var requestDataContent = new StringContent(requestDataJson, Encoding.UTF8, JsonMediaType);
        Add(requestDataContent, "data");

        // Explicit charset declaration
        var charsetContent = new StringContent(Encoding.UTF8.WebName, Encoding.UTF8, PlainTextMediaType);
        Add(charsetContent, "_charset_");

        // Add main content part
        if (part is Stream streamPart)
        {
            var streamContent = new StreamContent(streamPart);
            streamContent.Headers.ContentType = OctetStreamMediaType;
            Add(streamContent, partName, fileName);
        }
        else
        {
            var partJson = JsonSerializer.Serialize(part);
            var jsonContent = new StringContent(partJson, Encoding.UTF8, JsonMediaType);
            Add(jsonContent, partName);
        }
    }
}
