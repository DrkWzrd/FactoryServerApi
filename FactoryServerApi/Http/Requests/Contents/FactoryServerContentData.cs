using System.Text.Json;
using System.Text.Json.Serialization;

namespace FactoryServerApi.Http.Requests.Contents;

[JsonConverter(typeof(FactoryServerContentDataConverter))]
internal class FactoryServerContentData
{

    private readonly IReadOnlyDictionary<string, object?> _data;

    public FactoryServerContentData(IReadOnlyDictionary<string, object?> data)
    {
        _data = data;
    }

    public FactoryServerContentData(string propertyName, object? data)
    {
        _data = new Dictionary<string, object?>()
        {
            {propertyName, data ?? string.Empty },
        };
    }

    internal class FactoryServerContentDataConverter : JsonConverter<FactoryServerContentData>
    {
        public override FactoryServerContentData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(ref reader, options);
            return dict is null
                ? null :
                new FactoryServerContentData(dict);
        }

        public override void Write(Utf8JsonWriter writer, FactoryServerContentData value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value._data, options);
        }
    }
}
