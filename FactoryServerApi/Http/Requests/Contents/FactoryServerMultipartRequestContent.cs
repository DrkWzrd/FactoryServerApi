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
        Add(new StringContent("utf-8", Encoding.UTF8, MediaTypeHeaderValue.Parse(MediaTypeNames.Text.Plain)), "_charset_");
        if (part is Stream str)
        {
            var strContent = new StreamContent(str);
            strContent.Headers.ContentType = MediaTypeHeaderValue.Parse(MediaTypeNames.Application.Octet);
            Add(strContent, partName, fileName);
        }
        else
        {
            Add(new StringContent(JsonSerializer.Serialize(part), Encoding.UTF8, MediaTypeHeaderValue.Parse(MediaTypeNames.Application.Json)), partName);
        }
    }
}
