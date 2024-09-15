using System.Text.Json.Serialization;

namespace FactoryServerApi.Http;

public class SaveHeader
{
    public int SaveVersion { get; }
    public int BuildVersion { get; }
    public string SaveName { get; }
    public string MapName { get; }
    public string MapOptions { get; }
    public string SessionName { get; }
    public int PlayDurationSeconds { get; }
    public string SaveDateTime { get; }
    public bool IsModdedSave { get; }
    public bool IsEditedSave { get; }
    public bool IsCreativeModeEnabled { get; }

    [JsonConstructor]
    internal SaveHeader(
        int saveVersion,
        int buildVersion,
        string saveName,
        string mapName,
        string mapOptions,
        string sessionName,
        int playDurationSeconds,
        string saveDateTime,
        bool isModdedSave,
        bool isEditedSave,
        bool isCreativeModeEnabled)
    {
        SaveVersion = saveVersion;
        BuildVersion = buildVersion;
        SaveName = saveName;
        MapName = mapName;
        MapOptions = mapOptions;
        SessionName = sessionName;
        PlayDurationSeconds = playDurationSeconds;
        SaveDateTime = saveDateTime;
        IsModdedSave = isModdedSave;
        IsEditedSave = isEditedSave;
        IsCreativeModeEnabled = isCreativeModeEnabled;
    }
}
