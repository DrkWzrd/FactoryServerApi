using System.Text.Json.Serialization;

namespace FactoryServerApi;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FactoryServerPrivilegeLevel
{
    NotAuthenticated,  // The client is not authenticated
    Client,            // Client is authenticated with client privileges
    Administrator,     // Client is authenticated with admin privileges
    InitialAdmin,      // Client is authenticated as initial admin with privileges to claim the server
    APIToken           // Client is authenticated as a third-party application
}
