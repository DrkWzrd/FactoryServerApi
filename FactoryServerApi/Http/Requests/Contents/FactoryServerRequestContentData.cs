using System.Text.Json;
using System.Text.Json.Serialization;

namespace FactoryServerApi.Http.Requests.Contents;

[JsonConverter(typeof(FactoryServerContentDataConverter))]
internal sealed class FactoryServerRequestContentData
{

    private readonly IReadOnlyDictionary<string, object> _data;

    public FactoryServerRequestContentData(IReadOnlyDictionary<string, object> data)
    {
        _data = data;
    }

    public FactoryServerRequestContentData(string propertyName, object? data)
    {
        _data = new Dictionary<string, object>()
        {
            {propertyName, data ?? string.Empty },
        };
    }

    sealed class FactoryServerContentDataConverter : JsonConverter<FactoryServerRequestContentData>
    {
        public override FactoryServerRequestContentData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options);
            return dict is null
                ? null :
                new FactoryServerRequestContentData(dict);
        }

        public override void Write(Utf8JsonWriter writer, FactoryServerRequestContentData value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value._data, options);
        }
    }
}
