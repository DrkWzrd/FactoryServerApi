# Introduction

Dedicated Server API consists of two separate endpoints, both operating on the same port as the game server, which is normally 7777. 
If the server port is changed (through Engine.ini configuration file, or through -Port= command line argument), the API will listen on the
specified port instead.

Dedicated Server API endpoints:
- Dedicated Server Lightweight Query API is a simple UDP protocol designed for polling the server state through UDP continuously
with minimal overhead.
- Dedicated Server HTTPS API is a HTTPS server serving the requests to retrieve the more detailed state of the server, and control it's behavior.

# Lightweight Query API

Lightweight Query API is a lightweight API designed to allow continuously pulling data from the server and track server state changes.

## Protocol

Lightweight Query is a simple request-response UDP protocol with a message-based approach.
Note that all data used in the Lightweight Query API is always *Little Endian*, and not of network byte order.
Since the protocol is UDP, it is unreliable, which means some of the requests might be dropped or not receive responses, so instead
of waiting for the responses you should attempt to ping the server with a specific time interval.
Be wary of not trying to ping a dead Lightweight Query API for too long though, since you might end up triggering anti-DDoS measures on the host network.

The protocol consists of a simple message envelope format used for all messages:

| Offset (bytes)    | Data Type   | Name            | Description                                                                                |
| ----------------- |-------------| --------------- |--------------------------------------------------------------------------------------------|
| 0                 | uint16 (LE) | ProtocolMagic   | Always set to 0xF6D5                                                                       |
| 2                 | uint8       | MessageType     | Type ID of the payload in this envelope. See table down for details                        |
| 3                 | uint8       | ProtocolVersion | Version of the protocol desired by the client. Current protocol version is *1*             |
| ...               | Variant     | Payload         | Payload of the message. Specific to the message type.                                      |
| 3+sizeof(Payload) | uint8       | Terminator Byte | Always 0x1. Messages not ending with the terminator byte will be discarded.                |

The following message types are currently supported.

| Message Type | Name                  | Description                                                                                     |
| ------------ | --------------------- | ----------------------------------------------------------------------------------------------- |
| 0            | Poll Server State     | A request sent to the API to retrieve the information about the current server state            |
| 1            | Server State Response | A response sent by the server API back to the client containing the current state of the server |

### Poll Server State

| Offset (bytes) | Data Type   | Name            | Description                                                                                           |
| -------------- | ----------- | --------------- | ----------------------------------------------------------------------------------------------------- |
| 0              | uint64 (LE) | Cookie          | An unique identifier for this request to mark the response. Game Client uses current time in UE ticks |

### Server State

| Offset (bytes)         | Data Type        | Name             | Description                                                                                                                                     |
| ---------------------- | ---------------- | ---------------- | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| 0                      | uint64 (LE)      | Cookie           | An unique identifier for the request that triggered this response.                                                                              |
| 8                      | uint8            | ServerState      | Current state that the server is in. See Server States table for details.                                                                       |
| 9                      | uint32 (LE)      | ServerNetCL      | Game Changelist that the server is running. Changelist of the server must match the game client changelist for the client to be able to connect |
| 13                     | uint64 (LE)      | ServerFlags      | Flags describing this server. Most values are reserved or available for modded servers to use. See Server Flags for more information            |
| 21                     | uint8            | NumSubStates     | Number of Sub State entries in this response. Sub state entries can be used to detect changes in server state                                   |
| 22                     | ServerSubState[] | SubStates        | Sub state at index i. Number of sub states matches the value of NumSubStates                                                                    |
| 22+sizeof(SubStates)   | uint16 (LE)      | ServerNameLength | Length of the ServerName field in bytes                                                                                                         |
| 22+sizeof(SubStates)+1 | uint8[]          | ServerName       | UTF-8 encoded Server Name, as set by the player                                                                                                 |

Server Sub State is a sequential counter that is incremented by the server each time a state of the relevant system changes.
This allows determining a set of data that the API client needs to refresh when the server changes it, without having to continuously poll the HTTPS API.

ServerSubState structure is defined as following:

| Offset (bytes) | Data Type   | Name             | Description                                                                                                                                     |
| -------------- | ----------- | ---------------- | --------------------------------------------------------------------------- |
| 0              | uint8       | SubStateId       | ID of the sub state being changed. See Server Sub States for list of values |
| 8              | uint16 (LE) | SubStateVersion  | Current changelist of the sub state                                         |

### Server State Enum

| Enum Value | Enum Name | Description                                                                    |
| ---------- | --------- | ------------------------------------------------------------------------------ |
| 0          | Offline   | The server is offline. Servers will never send this as a response              |
| 1          | Idle      | The server is running, but no save is currently loaded                         |
| 2          | Loading   | The server is currently loading a map. In this state, HTTPS API is unavailable |
| 3          | Playing   | The server is running, and a save is loaded. Server is joinable by players     |

### Server Flags Bits

| Bit Index (LE) | Flag Name  | Description                                                                                |
| -------------- | ---------- | -------------------------------------------------------------------------                  |
| 0              | Modded     | The server is considered Modded. Vanilla clients will not try to connect to Modded servers |
| 1              | Custom1    | A flag with server-specific or context-specific meaning                                    |
| 2              | Custom2    | A flag with server-specific or context-specific meaning                                    |
| 3              | Custom3    | A flag with server-specific or context-specific meaning                                    |
| 4              | Custom4    | A flag with server-specific or context-specific meaning                                    |

### Server Sub States

The following sub states are currently defined by the vanilla dedicated server.
Sub states that are not known are not invalid, and should instead be silently discarded.

| Sub State ID | Sub State Name       | Description                                                                                                             |
| ------------ | -------------------- | ----------------------------------------------------------------------------------------------------------------------- |
| 0            | ServerGameState      | Game state of the server. Maps to QueryServerState HTTPS API function                                                   |
| 1            | ServerOptions        | Global options set on the server. Maps to GetServerOptions HTTPS API function                                           |
| 2            | AdvancedGameSettings | Advanced Game Settings in the currently loaded session. Maps to GetAdvancedGameSettings HTTPS API function              |
| 3            | SaveCollection       | List of saves available on the server for loading/downloading has changed. Maps to EnumerateSessions HTTPS API function |
| 4            | Custom1              | A value that can be used by the mods or custom servers. Not used by the vanilla clients or servers                      |
| 5            | Custom2              | A value that can be used by the mods or custom servers. Not used by the vanilla clients or servers                      |
| 6            | Custom3              | A value that can be used by the mods or custom servers. Not used by the vanilla clients or servers                      |
| 7            | Custom4              | A value that can be used by the mods or custom servers. Not used by the vanilla clients or servers                      |

## Flow

A client sends a message of type Poll Server State to the Server API with it's Cookie. When the server receives the message, it will send the 
Server State Response message to the relevant client, with the Cookie value on the response copied from the received request.

A client can continuously poll the server for the updates using that API without using much of server CPU, and investigate the changes
in the Server Sub States changelists to determine which subset of information it has cached locally from the HTTPS API could need to be re-fetched from the server.

## API Availability

Lightweight Server Query API is available at all times when the server is running, except for when it is starting up. When the server is performing
a save game load or a map change, the lightweight query API will retain it's availability, but will report Loading as the server status.
In that state, HTTPS API becomes temporarily unavailable until the blocking work on the server is finished.

# HTTPS API

Dedicated Server HTTPS API is designed for reliably retrieving data from the running dedicated server instance, and performing the server management.
It is available when the server has started up and not actively loading a save game or performing a map change. To check for the HTTPS API availability,
Lightweight Query API can be used.

## Certificate Validation and Encryption

HTTPS API is always wrapped into the TLS tunnel, even if the user did not provide the certificate for the Dedicated Server.

User Certificate will be looked up at the following path (where `$InstallRoot$` is the path where the Dedicated Server is installed):

| File Path                                                | File type               | Description                             |
|----------------------------------------------------------|-------------------------|-----------------------------------------|
| `$InstallRoot$/FactoryGame/Certificates/cert_chain.pem`  | Certificate Chain (PEM) | Certificate chain in PEM format         |
| `$InstallRoot$/FactoryGame/Certificates/private_key.pem` | Private Key (PEM)       | Certificate's private key in PEM format |

If no Certificate is provided by the user, Dedicated Server will generate it's own self-signed certificate and use it to encrypt
all traffic flowing through the HTTPS API. As such, the clients should be able to handle the HTTPS certificate being self-signed, recognize
that case, and handle it appropriately.

The game client, when presented with a self signed certificate from the Dedicated Server, will present it to the user and ask them to
manually confirm that the certificate in question is from a trusted authority. Once the user confirms it, the certificate is cached locally,
and is trusted for that specific server until the user revokes it or the server changes the certificate.

## Schema

HTTPS API is based on a simple JSON schema used to pass data to the functions executing on the server, and pass the responses back to the caller.
All Server API functions are always executed as POST requests, although certain query requests support being executed through the GET
requests, provided that they do not require any data to be provided to them.

### Request Schema

Content Type for requests should be set to application/json. Encoding should preferably be set to utf-8, but Dedicated Servers
support all encoding supported by the ICU localization library.

Request Object has the following properties:

| Property Name | Property Type | Description                                                                                           |
| ------------- | ------------- | ----------------------------------------------------------------------------------------------------- |
| function      | string        | Name of the API function to execute. Names of the API functions and their behavior is described below |
| data          | object        | Data to pass to the function to execute. Format of the object depends on the function being executed  |

Dedicated Server HTTPS API supports the following standard headers:

| Header Name      | Notes                                                                                                                |
| ---------------- | -------------------------------------------------------------------------------------------------------------------- |
| Content-Encoding | Optional. Only gzip and deflate are supported                                                                        |
| Authorization    | Required for most non-Authorization API functions. Only Bearer tokens are supported. See Authorization for more info |

The following Satisfactory-specific headers can be also be used in the request:

| Header Name            | Data Type     | Description                                                                                 |
| X-FactoryGame-PlayerId | Hex String    | Hex-encoded byte array encoding the ID of the player on behalf of which the request is made | 

X-FactoryGame-PlayerId header is only needed to obtain the server join/encryption tickets used for joining the server,
and it's format is highly specific to the Satisfactory version running, Unreal Engine version, and the Online Backend used by the player.

Generally, first byte of the ID will be type of the Online Backend used (1 for Epic Games Store, 6 for Steam, see values in UE's EOnlineServices type),
and the following bytes are specific to the Online Backend, but will generally represent the player account ID.
For Steam for example, it would be a big-endian uint64 representing the player's SteamID64, and for Epic, it would be HEX-encoded EOS ProductUserId string.

### Response Schema

Dedicated Server can return a variety of different HTTP status codes, most prominent ones are described here:

| Status Code | Status Code Name  | Description                                                                                                                                                      |
| ----------- | ----------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 200         | Ok                | The function has been executed successfully. The response body will either be a Error Response or a Success Response                                             |
| 201         | Created           | The function has been executed and returned no error. Returned by some functions to indicate that a new file has been created, such as UploadSaveGame            |
| 202         | Accepted          | The function has been executed, but is still being processed. This is returned by some functions with side deffects, such as LoadGame                            |
| 204         | No Content        | The function has been executed successfully, but returned no data (and no error)                                                                                 |
| 400         | Bad Request       | Only returned when the request body was failed to be parsed as valid JSON or multipart request. In other cases, error response bad_request is used               |
| 401         | Denied            | Authentication token is missing, cannot be parsed, or has expired                                                                                                |
| 403         | Forbidden         | Provided authentication does not allow executing the provided function, or when function requiring authentication is called without one                          |
| 404         | Not Found         | The specified function cannot be found, or when the function cannot find the specified resource in some cases (for example, for DownloadSaveGame)                |
| 415         | Unsupported Media | Specified charset or content encoding is not supported, or multipart data is malformed                                                                           |
| 500         | Server Error      | An internal server error has occurred when executing the function                                                                                                |

Content Type of the server response will be set to application/json and utf-8 encoding.
Depending on the outcome of the operation, it might return either an error response or a data response.

Error Response has the following structure:

| Property Name | Property Type | Description                                                                                           |
| ------------- | ------------- | ----------------------------------------------------------------------------------------------------- |
| errorCode     | string        | Machine-friendly code indicating the type of the error that the executed function returned            |
| errorMessage  | string?       | Optional. Human-friendly error message explaining the error                                           |
| errorData     | object?       | Optional. Additional information about the error, for example, list of parameters that are missing    |

Success Response has the following structure:

| Property Name | Property Type | Description                                                                                           |
| ------------- | ------------- | ----------------------------------------------------------------------------------------------------- |
| data          | object?       | Data returned by the function executed. Type depends on the function the request performed            |

### File Responses

Certain Dedicated Server API functions can respond with a file attachment instead of using the standard Server Response JSON schema.
Such responses can be distinguished by the presence of the Content-Disposition header, indicating that the Content-Type and body represent
a file attachment and not a standard Server API response body.

Such functions can still return normal server response in case of Error Response.
Currently the only function that utilizies the File Response functionality is DownloadSaveGame, which returns the save game file as a response 
without any additional Server API metadata attachments.

### Multipart Requests

Certain Server API functions can take both Server API request payload and a file attachment. Such functions should use multipart/form-data 
Content-Type, and encode both the payload and the Server API request body as separate multipart parts.

Multipart Part named "data" should be present in all multipart requests, and encode the request object JSON with the same schema and
restrictions as the normal non-multipart requests. The charset should be provided as a separate multipart part with a special name "_charset_",
and contain the name of the charset used for encoding the Server API request as a plain text.

Names of other multipart attachments are specific to the functions using multipart requests.
Currently multipart requests are only used by UploadSaveGame function for uploading save game files.

## Authentication

Dedicated Server API requires authentication for most of it's functions. Authentication format used are Bearer tokens, which are issued
by the Dedicated Server when using certain API functions that require no Authentication (such as PasswordlessLogin), or functions that require
additional security verification (such as PasswordLogin). Tokens generated by these functions are short-lived and are bound to the specific player account.

Authentication Tokens internally consist of two parts separated by the dot character ('.'):
- Base64-encoded JSON token payload
- HEX-encoded Fingerprint

JSON token payload can be retrieved to determine the privilege level granted by the token, while fingerprint part is used by the server
to check the validity of the token and whenever it can be used currently.

Internal Authentication Token Payload:
| Property Name | Property Type | Description                                                                                           |
| ------------- | ------------- | ----------------------------------------------------------------------------------------------------- |
| pl            | string        | Privilege Level granted by this token. See possible values below                                      |

Possible Privilege Level values:

| Privilege Level  | Description                                                                  |
| ---------------- | ---------------------------------------------------------------------------- |
| NotAuthenticated | The client is not Authenticated                                              |
| Client           | Client is Authenticated with Client privileges                               |
| Administrator    | Client is Authenticated with Admin privileges                                |
| InitialAdmin     | Client is Authenticated as Initial Admin with privileges to Claim the server |
| APIToken         | Client is Authenticated as Third Party Application                           |

The following functions are used by the game client to perform player authentication:

| Function Name             | Description                                                                         |
| ------------------------- | ----------------------------------------------------------------------------------- |
| PasswordlessLogin         | Attempts logging in as a player without a password.                                 |
| PasswordLogin             | Attempts logging in as a player with a password.                                    |
| VerifyAuthenticationToken | Checks if the provided Authentication token is valid. Returns Ok if valid           |

Third Party Applications should NOT use PasswordLogin or PasswordlessLogin, and should instead rely on the Application Tokens.

Application tokens do not expire, and are granted by issuing the command `server.GenerateAPIToken` in the Dedicated Server console.
The generated token can then be passed to the Authentication header with Bearer type to perform any Server API requests on the behalf of the server.

Application tokens generated previously can still be pruned using `server.InvalidateAPITokens` console command.

Authentication requirement can be lifted for locally running Dedicated Server instances serving on the loopback network adapter.
To allow unrestricted Dedicated Server API access on the localhost, set `FG.DedicatedServer.AllowInsecureLocalAccess` console variable to `1`.
It can be performed automatically using the following command line argument:
`-ini:Engine:[SystemSettings]:FG.DedicatedServer.AllowInsecureLocalAccess=1`

## API Functions

The following functions are currently available in the vanilla Dedicated Server

### HealthCheck

Performs a health check on the Dedicated Server API. Allows passing additional data between Modded Dedicated Server and Modded Game Client.
This function requires no Authentication.

Function Request Data:

| Property Name        | Property Type       | Description                                                                                           |
| -------------------- | ------------------- | ----------------------------------------------------------------------------------------------------- |
| ClientCustomData     | string              | Custom Data passed from the Game Client or Third Party service. Not used by vanilla Dedicated Servers |

Function Response Data:

| Property Name        | Property Type       | Description                                                                                                                           |
| -------------------- | ------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| Health               | string              | "healthy" if tick rate is above ten ticks per second, "slow" otherwise                                                                |
| ServerCustomData     | string              | Custom Data passed from the Dedicated Server to the Game Client or Third Party service. Vanilla Dedicated Server returns empty string |

### VerifyAuthenticationToken

Verifies the Authentication token provided to the Dedicated Server API. Returns No Content if the provided token is valid.
This function does not require input parameters and does not return any data.

### PasswordlessLogin

Attempts to perform a passwordless login to the Dedicated Server as a player. Passwordless login is possible if the Dedicated Server is not claimed,
or if Client Protection Password is not set for the Dedicated Server.
This function requires no Authentication.

Function Request Data:

| Property Name         | Property Type       | Description                                                                                               |
| --------------------- | ------------------- | --------------------------------------------------------------------------------------------------------- |
| MinimumPrivilegeLevel | string              | Minimum privilege level to attempt to acquire by logging in. See Privilege Level enum for possible values |

Function Response Data:

| Property Name        | Property Type       | Description                                                                                                                           |
| -------------------- | ------------------- | ------------------------------------------------- |
| AuthenticationToken  | string              | Authentication Token in case login is successful  |

Possible errors:

| Error Code                      | Description                                                                                             |
| ------------------------------- | ------------------------------------------------------------------------------------------------------- |
| passwordless_login_not_possible | Passwordless login is not currently possible for this Dedicated Server                                  |

### PasswordLogin

Attempts to log in to the Dedicated Server as a player using either Admin Password or Client Protection Password.
This function requires no Authentication.

Function Request Data:

| Property Name         | Property Type       | Description                                                                                               |
| --------------------- | ------------------- | --------------------------------------------------------------------------------------------------------- |
| MinimumPrivilegeLevel | string              | Minimum privilege level to attempt to acquire by logging in. See Privilege Level enum for possible values |
| Password              | string              | Password to attempt to log in with, in plaintext                                                          |

Function Response Data:

| Property Name        | Property Type       | Description                                                                                                                           |
| -------------------- | ------------------- | ------------------------------------------------- |
| AuthenticationToken  | string              | Authentication Token in case login is successful  |

Possible errors:

| Error Code                      | Description                                                                                             |
| ------------------------------- | ------------------------------------------------------------------------------------------------------- |
| wrong_password                  | Provided Password did not match any of the passwords set for this Dedicated Server                      |

### QueryServerState

Retrieves the current state of the Dedicated Server. Does not require any input parameters.

Function Response Data:

| Property Name   | Property Type | Description                                                                                           |
| --------------- | ------------- | ----------------------------------------------------------------------------------------------------- |
| ServerGameState | string        | Current game state of the server                                                                      |

ServerGameState:

| Property Name       | Property Type | Description                                                                                           |
| ------------------- | ------------- | ----------------------------------------------------------------------------------------------------- |
| ActiveSessionName   | string        | Name of the currently loaded game session                                                             |
| NumConnectedPlayers | integer       | Number of the players currently connected to the Dedicated Server                                     |
| PlayerLimit         | integer       | Maximum number of the players that can be connected to the Dedicated Server                           |
| TechTier            | integer       | Maximum Tech Tier of all Schematics currently unlocked                                                |
| ActiveSchematic     | string        | Schematic currently set as Active Milestone                                                           |
| GamePhase           | string        | Current game phase. None if no game is running                                                        |
| IsGameRunning       | boolean       | True if the save is currently loaded, false if the server is waiting for the session to be created    |
| TotalGameDuration   | integer       | Total time the current save has been loaded, in seconds                                               |
| IsGamePaused        | boolean       | True if the game is paused. If the game is paused, total game duration does not increase              |
| AverageTickRate     | float         | Average tick rate of the server, in ticks per second                                                  |
| AutoLoadSessionName | string        | Name of the session that will be loaded when the server starts automatically                          |

### GetServerOptions

Retrieves currently applied server options and server options that are still pending application (because of needing session or server restart)
Does not require input parameters.

Function Response Data:

| Property Name        | Property Type       | Description                                                                                           |
| -------------------- | ------------------- | ----------------------------------------------------------------------------------------------------- |
| ServerOptions        | map<string, string> | All current server option values. Key is the name of the option, and value is it's stringified value  |
| PendingServerOptions | map<string, string> | Server option values that will be applied when the session or server restarts                         |

### GetAdvancedGameSettings

Retrieves currently applied advanced game settings. Does not require input parameters.

Function Response Data:

| Property Name        | Property Type       | Description                                                                                           |
| -------------------- | ------------------- | ----------------------------------------------------------------------------------------------------- |
| CreativeModeEnabled  | boolean             | True if Advanced Game Settings are enabled for the currently loaded session                           |
| AdvancedGameSettings | map<string, string> | Values of Advanced Game Settings. Key is the name of the setting, and value is it's stringified value |

### ApplyAdvancedGameSettings

Applies new values to the provided Advanced Game Settings properties. Will automatically enable Advanced Game Settings
for the currently loaded save if they are not enabled already.

Function Request Data:

| Property Name                | Property Type       | Description                                                                                           |
| ---------------------------- | ------------------- | ----------------------------------------------------------------------------------------------------- |
| AppliedAdvancedGameSettings  | map<string, string> | Key is the name of the Advanced Game Setting, and the Value is the new setting value as string        |

### ClaimServer

Claims this Dedicated Server if it is not claimed. Requires InitialAdmin privilege level, which can only be acquired by attempting passwordless login
while the server does not have an Admin Password set, e.g. it is not claimed yet.
Function does not return any data in case of success, and the server is claimed. The client should drop InitialAdmin privileges after that
and use returned AuthenticationToken instead, and update it's cached server game state by calling QueryServerState.

Function Request Data:

| Property Name        | Property Type       | Description                                                                                           |
| -------------------- | ------------------- | ----------------------------------------------------------------------------------------------------- |
| ServerName           | string              | New name of the Dedicated Server                                                                      |
| AdminPassword        | string              | Admin Password to set on the Dedicated Server, in plaintext                                           |

Function Response Data:

| Property Name        | Property Type       | Description                                                                                           |
| -------------------- | ------------------- | ----------------------------------------------------------------------------------------------------- |
| AuthenticationToken  | string              | New Authentication Token that the Caller should use to drop Initial Admin privileges                  |

Possible errors:

| Error Code       | Description                                                                |
| ---------------- | -------------------------------------------------------------------------- |
| server_claimed   | Server has already been claimed                                            |

### RenameServer

Renames the Dedicated Server once it has been claimed. Requires Admin privileges.
Function does not return any data on success.

Function Request Data:

| Property Name        | Property Type       | Description                                                                                           |
| -------------------- | ------------------- | ----------------------------------------------------------------------------------------------------- |
| ServerName           | string              | New name of the Dedicated Server                                                                      |

Possible errors:

| Error Code         | Description                                                                |
| ------------------ | -------------------------------------------------------------------------- |
| server_not_claimed | Server has not been claimed yet. Use ClaimServer function instead          |

### SetClientPassword

Updates the currently set Client Protection Password. This will invalidate all previously issued Client authentication tokens.
Pass empty string to remove the password, and let anyone join the server as Client.
Requres Admin privileges. Function does not return any data on success.

Function Request Data:

| Property Name        | Property Type       | Description                                                                                           |
| -------------------- | ------------------- | ----------------------------------------------------------------------------------------------------- |
| Password             | string              | Client Password to set on the Dedicated Server, in plaintext                                          |

Possible errors:

| Error Code         | Description                                                                                        |
| ------------------ | -------------------------------------------------------------------------------------------------- |
| server_not_claimed | Server has not been claimed yet. Use ClaimServer function instead before calling SetClientPassword |
| password_in_use    | Same password is already used as Admin Password                                                    |

### SetAdminPassword

Updates the currently set Admin Password. This will invalidate all previously issued Client and Admin authentication tokens.
Requires Admin privileges. Function does not return any data on success.

Function Request Data:

| Property Name        | Property Type       | Description                                                                                           |
| -------------------- | ------------------- | ----------------------------------------------------------------------------------------------------- |
| Password             | string              | Admin Password to set on the Dedicated Server, in plaintext                                           |
| AuthenticationToken  | string              | New Admin authentication token to use, since the token used for this request will become invalidated  |

Possible errors:

| Error Code                  | Description                                                                                        |
| --------------------------- | -------------------------------------------------------------------------------------------------- |
| server_not_claimed          | Server has not been claimed yet. Use ClaimServer function instead                                  |
| cannot_reset_admin_password | Attempt to set Password to empty string. Admin Password cannot be reset                            |
| password_in_use             | Same password is already used as Client Protection Password                                        |

### SetAutoLoadSessionName

Updates the name of the session that the Dedicated Server will automatically load on startup. Does not change currently loaded session.
Requires Admin privileges. Function does not return any data on success.

Function Request Data:

| Property Name        | Property Type       | Description                                                                                           |
| -------------------- | ------------------- | ----------------------------------------------------------------------------------------------------- |
| SessionName          | string              | Name of the session to automatically load on Dedicated Server startup                                 |

### RunCommand

Runs the given Console Command on the Dedicated Server, and returns it's output to the Console. Requires Admin privileges.

Function Request Data:

| Property Name        | Property Type       | Description                                                                                           |
| -------------------- | ------------------- | ----------------------------------------------------------------------------------------------------- |
| Command              | string              | Command Line to run on the Dedicated Server                                                           |

Function Response Data:

| Property Name        | Property Type       | Description                                                                                           |
| -------------------- | ------------------- | ----------------------------------------------------------------------------------------------------- |
| CommandResult        | string              | Output of the command executed, with \n used as line separator                                        |

### Shutdown

Shuts down the Dedicated Server. If automatic restart script is setup, this allows restarting the server to apply new settings or update.
Requires Admin privileges. Shutdowns initiated by remote hosts are logged with their IP and their token.
Function does not return any data on success, and does not take any parameters.

### ApplyServerOptions

Applies new Server Options to the Dedicated Server. Requires Admin privileges.
Function does not return any data on success.

Function Request Data:

| Property Name                | Property Type       | Description                                                                                   |
| ---------------------------- | ------------------- | --------------------------------------------------------------------------------------------- |
| UpdatedServerOptions         | map<string, string> | Key is the name of the Server Option, and the Value is the new value as string                |

### CreateNewGame

Creates a new session on the Dedicated Server, and immediately loads it. HTTPS API becomes temporarily unavailable when map loading is in progress   |
Function does not return any data on success.

| Property Name        | Property Type       | Description                                                                                           |
| -------------------- | ------------------- | ----------------------------------------------------------------------------------------------------- |
| NewGameData          | ServerNewGameData   | Parameters needed to create new game session                                                          |

ServerNewGameData:

| Property Name               | Property Type       | Description                                                                                                |
| --------------------------- | ------------------- | ---------------------------------------------------------------------------------------------------------- |
| SessionName                 | string              | Name of the session to create                                                                              |
| MapName                     | string              | Path Name to the Map Package to use as a map. If not specified, default level                              |
| StartingLocation            | string              | Name of the starting location to use. Leaving it empty will use random starting location                   |
| SkipOnboarding              | boolean             | Whenever the Onboarding should be skipped. Currently Onboarding is always skipped on the Dedicated Servers |
| AdvancedGameSettings        | map<string, string> | Advanced Game Settings to apply to the newly created session                                               |
| CustomOptionsOnlyForModding | map<string, string> | Custom Options to pass to the newly created session URL. Not used by vanilla Dedicated Servers             |

### SaveGame

Saves the currently loaded session into the new save game file named as the argument.
Requires Admin privileges. SaveName might be changed to satisfy file system restrictions on file names.
Function does not return any data on success.

Function Request Data:

| Property Name        | Property Type       | Description                                                                                           |
| -------------------- | ------------------- | ----------------------------------------------------------------------------------------------------- |
| SaveName             | string              | Name of the save game file to create. Passed name might be sanitized                                  |

Possible errors:

| Error Code         | Description                                                                                        |
| ------------------ | -------------------------------------------------------------------------------------------------- |
| save_game_failed   | Failed to save the game. Additional information might be availble in errorMessage property         |

### DeleteSaveFile

Deletes the existing save game file from the server. Requires Admin privileges. SaveName might be changed to satisfy file system
restrictions on file names. Function does not return any data on success.

Function Request Data:

| Property Name        | Property Type       | Description                                                                                           |
| -------------------- | ------------------- | ----------------------------------------------------------------------------------------------------- |
| SaveName             | string              | Name of the save game file to delete. Passed name might be sanitized                                  |

Possible errors:

| Error Code              | Description                                                                                             |
| ----------------------- | ------------------------------------------------------------------------------------------------------- |
| delete_save_file_failed | Failed to delete the save game file. Additional information might be available in errorMessage property |

### DeleteSaveSession

Deletes all save files belonging to the specific session name. Requires Admin privileges. SessionName must be
a valid session name with at least one saved save game file belonging to it.
Function does not return any data on success.

Function Request Data:

| Property Name        | Property Type       | Description                                                         |
| -------------------- | ------------------- | ------------------------------------------------------------------- |
| SessionName          | string              | Name of the save session to delete                                  |

Possible errors:

| Error Code                 | Description                                                                                             |
| -------------------------- | ------------------------------------------------------------------------------------------------------- |
| session_not_found          | Failed to find any save game files belonging to the given session                                       |
| delete_save_session_failed | Failed to delete save session files. Additional information might be available in errorMessage property |

### EnumerateSessions

Enumerates all save game files available on the Dedicated Server. Requires Admin privileges. Function does not require any additional parameters.

Function Response Data:

| Property Name        | Property Type            | Description                                                         |
| -------------------- | ------------------------ | ------------------------------------------------------------------- |
| Sessions             | array<SessionSaveStruct> | List of sessions available on the Dedicated Server                  |
| CurrentSessionIndex  | integer                  | Index of the currently selected session in the list                 |

Possible errors:

| Error Code                 | Description                                                                                             |
| -------------------------- | ------------------------------------------------------------------------------------------------------- |
| enumerate_sessions_failed  | Failed to enumerate save sessions. Additional information might be available in errorMessage property   |

SessionSaveStruct:

| Property Name               | Property Type       | Description                                                                                                |
| --------------------------- | ------------------- | ---------------------------------------------------------------------------------------------------------- |
| SessionName                 | string              | Name of the save session                                                                                   |
| SaveHeaders                 | array<SaveHeader>   | All save game files belonging to this session                                                              |

SaveHeader:

| Property Name               | Property Type       | Description                                                                                                |
| --------------------------- | ------------------- | ---------------------------------------------------------------------------------------------------------- |
| SaveVersion                 | integer             | Version of the Save Game format this file was saved with                                                   |
| BuildVersion                | integer             | Changelist of the game or dedicated server this file was saved by                                          |
| SaveName                    | string              | Name of the save game file in the filesystem                                                               |
| MapName                     | string              | Path to the map package this save game file is based on                                                    |
| MapOptions                  | string              | Additional Map URL options this save game was saved with                                                   |
| SessionName                 | string              | Custom Options to pass to the newly created session URL. Not used by vanilla Dedicated Servers             |
| PlayDurationSeconds         | integer             | Amount of seconds the game has been running for in total since the session was created                     |
| SaveDateTime                | string              | Date and time on which the save game file was saved                                                        |
| IsModdedSave                | boolean             | True if this save game file was saved with mods                                                            |
| IsEditedSave                | boolean             | True if this save game file has been edited by third party tools                                           |
| IsCreativeModeEnabled       | boolean             | True if Advanced Game Settings are enabled for this save game                                              |

### LoadGame

Loads the save game file by name, optionally with Advanced Game Settings enabled. Requires Admin privileges.
Dedicated Server HTTPS API will become temporarily unavailable when save game is being loaded.
Function does not return any data on succcess.

Function Request Data:

| Property Name               | Property Type            | Description                                                                 |
| --------------------------- | ------------------------ | --------------------------------------------------------------------------- |
| SaveName                    | string                   | Name of the save game file to load                                          |
| EnableAdvancedGameSettings  | boolean                  | True if save game file should be loaded with Advanced Game Settings enabled |

Possible errors:

| Error Code                 | Description                                                                                             |
| -------------------------- | ------------------------------------------------------------------------------------------------------- |
| save_game_load_failed      | Failed to find the save game file with the given name on the Dedicated Server                           |

### UploadSaveGame

Uploads save game file to the Dedicated Server with the given name, and optionally immediately loads it.
Requires Admin privileges. If save file is immediately loaded, Dedicated Server HTTPS API will become temporarily unavailable until save game is loaded.
This function does not return any data on success.

This function requires multipart-encoded form data as it's body. The following multipart part names are allowed:

| Multipart Field            | Description                                                                                              |
| -------------------------- | -------------------------------------------------------------------------------------------------------- |
| data                       | Standard Request Data body for this request, encoded as utf-8 JSON. Note that this includes the envelope |
| saveGameFile               | File attachment containing the save game file. Contents of the file will be validated on the server      |

Function Request Data:

| Property Name               | Property Type            | Description                                                                 |
| --------------------------- | ------------------------ | --------------------------------------------------------------------------- |
| SaveName                    | string                   | Name of the save game file to create on the Dedicated Server                |
| LoadSaveGame                | boolean                  | True if save game file should be immediately loaded by the Dedicated Server |
| EnableAdvancedGameSettings  | boolean                  | True if save game file should be loaded with Advanced Game Settings enabled |

Possible errors:

| Error Code                 | Description                                                                                             |
| -------------------------- | ------------------------------------------------------------------------------------------------------- |
| invalid_save_game          | Invalid save game file encoding, malformed header or corrupted contents                                 |
| unsupported_save_game      | Save game file is too old to be loaded by the Dedicated Server, or is too new                           |
| file_save_failed           | Failed to save the save game file to the underlying file system                                         |
| save_game_load_failed      | Failed to find the created save game file                                                               |

### DownloadSaveGame

Downloads save game with the given name from the Dedicated Server. Requires Admin privileges.
This function responds with the file attachment containing the save game file on success, and with normal error response in case of error.

Function Request Data:

| Property Name               | Property Type            | Description                                                                 |
| --------------------------- | ------------------------ | --------------------------------------------------------------------------- |
| SaveName                    | string                   | Name of the save game file to download from the Dedicated Server            |

Possible errors:

| Error Code                 | Description                                                                                             |
| -------------------------- | ------------------------------------------------------------------------------------------------------- |
| file_not_found             | Save game file with the provided name is not found on the Dedicated Server                              |
