using System.Text.Json;
using System.Text.Json.Serialization;

namespace FactoryServerApi;

[JsonConverter(typeof(MapNameJsonConverter))]
public class MapName
{
    public static MapName PersistentLevel { get; } = new MapName("Persistent_Level");

    private readonly string _string;

    private MapName(string str)
    {
        _string = str;
    }

    public static MapName CustomMap(string mapName)
    {
        return new MapName(mapName);
    }

    public static implicit operator string?(MapName? loc)
    {
        return loc?._string;
    }
    internal class MapNameJsonConverter : JsonConverter<MapName>
    {
        public override MapName? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Read the JSON string value and convert it to MapName
            string? value = reader.GetString();
            return value is null
                ? null
                : new MapName(value);
        }

        public override void Write(Utf8JsonWriter writer, MapName value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value._string);
        }
    }
}
