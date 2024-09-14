using System.Net.Http.Json;
using System.Text.Json;

namespace FactoryServerApi.Http.Requests.Contents;

public abstract class FactoryServerMultipartContent : MultipartFormDataContent
{

    public string Function { get; }

    protected FactoryServerMultipartContent(string function, IFactoryServerContentData data, string partName, object part)
    {
        Function = function;
        Headers.ContentEncoding.Clear();
        var dictionary = new Dictionary<string, object>()
        {
            {"function", Function },
            {"data", data },
        };

        Add(JsonContent.Create(dictionary, options: FactoryServerContent.FactoryServerJsonOptions), "data");
        Add(new StringContent("utf-8"), "_charset_");
        if (part is Stream str)
        {
            Add(new StreamContent(str), partName);
        }
        else
        {
            Add(new StringContent(JsonSerializer.Serialize(part, FactoryServerContent.FactoryServerJsonOptions)));
        }

    }
}
