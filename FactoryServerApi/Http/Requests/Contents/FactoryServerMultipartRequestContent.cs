using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace FactoryServerApi.Http.Requests.Contents;

public abstract class FactoryServerMultipartRequestContent : MultipartFormDataContent
{

    public string Function { get; }

    private protected FactoryServerMultipartRequestContent(string function, FactoryServerRequestContentData data, string partName, string fileName, object part)
    {
        Function = function;
        Headers.ContentEncoding.Clear();
        var requestData = new Dictionary<string, object>()
        {
            {"function", Function },
            {"data", data },
        };
        var requestDataContent = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, MediaTypeHeaderValue.Parse(MediaTypeNames.Application.Json));
        Add(requestDataContent, "data");
        var explicitCharsetContent = new StringContent(Encoding.UTF8.WebName, Encoding.UTF8, MediaTypeHeaderValue.Parse(MediaTypeNames.Text.Plain));
        Add(explicitCharsetContent, "_charset_");
        if (part is Stream str)
        {
            var fileContent = new StreamContent(str);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(MediaTypeNames.Application.Octet);
            Add(fileContent, partName, fileName);
        }
        else
        {
            var additionalContent = new StringContent(JsonSerializer.Serialize(part), Encoding.UTF8, MediaTypeHeaderValue.Parse(MediaTypeNames.Application.Json));
            Add(additionalContent, partName);
        }
    }
}
