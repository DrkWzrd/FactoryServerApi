﻿using System.Text.Json.Serialization;

namespace FactoryServerApi.Http.Responses;

public class GetAdvancedGameSettingsResponseData
{
    public bool CreativeModeEnabled { get; }
    public IReadOnlyDictionary<string, string> AdvancedGameSettings { get; }

    [JsonConstructor]
    internal GetAdvancedGameSettingsResponseData(
        bool creativeModeEnabled,
        Dictionary<string, string> advancedGameSettings)
    {
        CreativeModeEnabled = creativeModeEnabled;
        AdvancedGameSettings = advancedGameSettings;
    }
}
