using System.Text.Json;
using System.Text.Json.Serialization;

namespace FactoryServerApi.Http;

[JsonConverter(typeof(StartingLocationJsonConverter))]
public class StartingLocation
{
    private static readonly Lazy<StartingLocation> _grassFields = new(() => new StartingLocation(nameof(GrassFields)));
    private static readonly Lazy<StartingLocation> _rockyDesert = new(() => new StartingLocation(nameof(RockyDesert)));
    private static readonly Lazy<StartingLocation> _northernForest = new(() => new StartingLocation(nameof(NorthernForest)));
    private static readonly Lazy<StartingLocation> _duneDesert = new(() => new StartingLocation(nameof(DuneDesert)));

    public static StartingLocation GrassFields => _grassFields.Value;
    public static StartingLocation RockyDesert => _rockyDesert.Value;
    public static StartingLocation NorthernForest => _northernForest.Value;
    public static StartingLocation DuneDesert => _duneDesert.Value;

    private readonly string _string;

    private StartingLocation(string str)
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
            return value is null
                ? null
                : new StartingLocation(value);
        }

        public override void Write(Utf8JsonWriter writer, StartingLocation value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value._string);
        }
    }
}
