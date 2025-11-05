using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

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

        var requestData = new
        {
            function = Function,
            data
        };

        string requestDataJson = JsonSerializer.Serialize(requestData);
        StringContent requestDataContent = new(requestDataJson, Encoding.UTF8, JsonMediaType);
        Add(requestDataContent, "data");

        StringContent charsetContent = new(Encoding.UTF8.WebName, Encoding.UTF8, PlainTextMediaType);
        Add(charsetContent, "_charset_");

        if (part is Stream streamPart)
        {
            StreamContent streamContent = new(streamPart);
            streamContent.Headers.ContentType = OctetStreamMediaType;
            Add(streamContent, partName, fileName);
        }
        else
        {
            string partJson = JsonSerializer.Serialize(part);
            StringContent jsonContent = new(partJson, Encoding.UTF8, JsonMediaType);
            Add(jsonContent, partName);
        }
    }
}
