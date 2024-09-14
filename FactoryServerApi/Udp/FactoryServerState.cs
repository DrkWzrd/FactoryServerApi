using System.Text.Json.Serialization;

namespace FactoryServerApi.Udp;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FactoryServerState : byte
{
    Offline = 0, //The server is offline. Servers will never send this as a response
    Idle = 1, //The server is running, but no save is currently loaded
    Loading = 2, //The server is currently loading a map. In this state, HTTPS API is unavailable,
    Playing = 3, //The server is running, and a save is loaded. Server is joinable by players
}