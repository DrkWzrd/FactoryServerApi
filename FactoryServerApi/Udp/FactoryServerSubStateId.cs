using System.Text.Json.Serialization;

namespace FactoryServerApi.Udp;

/// <summary>
/// Enum representing various sub-states of a server in the Lightweight Query API.
/// These sub-states help track specific changes in server state and map to certain
/// HTTPS API functions or custom values defined by modded servers.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FactoryServerSubStateId : byte
{
    /// <summary>
    /// Game state of the server. Maps to QueryServerState HTTPS API function.
    /// </summary>
    ServerGameState = 0,

    /// <summary>
    /// Global options set on the server. Maps to GetServerOptions HTTPS API function.
    /// </summary>
    ServerOptions = 1,

    /// <summary>
    /// Advanced game settings in the currently loaded session. Maps to GetAdvancedGameSettings HTTPS API function.
    /// </summary>
    AdvancedGameSettings = 2,

    /// <summary>
    /// List of saves available on the server has changed. Maps to EnumerateSessions HTTPS API function.
    /// </summary>
    SaveCollection = 3,

    /// <summary>
    /// Custom value that can be used by mods or custom servers. Not used by vanilla clients or servers.
    /// </summary>
    Custom1 = 4,

    /// <summary>
    /// Custom value that can be used by mods or custom servers. Not used by vanilla clients or servers.
    /// </summary>
    Custom2 = 5,

    /// <summary>
    /// Custom value that can be used by mods or custom servers. Not used by vanilla clients or servers.
    /// </summary>
    Custom3 = 6,

    /// <summary>
    /// Custom value that can be used by mods or custom servers. Not used by vanilla clients or servers.
    /// </summary>
    Custom4 = 7
}
