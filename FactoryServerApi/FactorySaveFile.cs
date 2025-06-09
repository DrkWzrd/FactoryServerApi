using System.Text.Json.Serialization;

namespace FactoryServerApi;

public class FactorySaveFile
{
    public int SaveVersion { get; }
    public int BuildVersion { get; }
    public string SaveName { get; }
    public string? SaveLocationInfo { get; }
    public string MapName { get; }
    public string MapOptions { get; }
    public string SessionName { get; }
    public int PlayDurationSeconds { get; }
    public string SaveDateTime { get; }
    public bool IsModdedSave { get; }
    public bool IsEditedSave { get; }
    public bool IsCreativeModeEnabled { get; }

    [JsonConstructor]
    internal FactorySaveFile(
        int saveVersion,
        int buildVersion,
        string saveName,
        string? saveLocationInfo,
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
        SaveLocationInfo = saveLocationInfo;
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
