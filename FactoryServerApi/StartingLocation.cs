using System.Text.Json;
using System.Text.Json.Serialization;

namespace FactoryServerApi;

[JsonConverter(typeof(StartingLocationJsonConverter))]
public class StartingLocation
{
    public static readonly StartingLocation Empty = new();

    public static StartingLocation GrassFields { get; } = new StartingLocation(nameof(GrassFields));
    public static StartingLocation RockyDesert { get; } = new StartingLocation(nameof(RockyDesert));
    public static StartingLocation NorthernForest { get; } = new StartingLocation(nameof(NorthernForest));
    public static StartingLocation DuneDesert { get; } = new StartingLocation(nameof(DuneDesert));

    private readonly string? _string;

    private StartingLocation(string? str = null)
    {
        _string = str;
    }

    public static StartingLocation CustomLocation(string strLoc)
    {
        return new StartingLocation(strLoc);
    }

    public static implicit operator string?(StartingLocation? loc)
    {
        return loc?._string;
    }
    internal class StartingLocationJsonConverter : JsonConverter<StartingLocation>
    {
        public override StartingLocation? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Read the JSON string value and convert it to StartingLocation
            string? value = reader.GetString();
            return string.IsNullOrWhiteSpace(value)
                ? Empty
                : new StartingLocation(value);
        }

        public override void Write(Utf8JsonWriter writer, StartingLocation value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value._string);
        }
    }
}
