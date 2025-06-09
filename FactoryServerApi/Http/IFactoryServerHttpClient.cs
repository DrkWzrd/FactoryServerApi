using FactoryServerApi.Http.Responses;

namespace FactoryServerApi.Http;

public interface IFactoryServerHttpClient
{
    FactoryGamePlayerId? PlayerId { get; }
    string? AuthenticationToken { get; }

    Task SetPlayerIdAsync(FactoryGamePlayerId playerId, CancellationToken cancellationToken = default);

    Task ClearPlayerIdAsync(CancellationToken cancellationToken = default);

    Task SetAuthenticationTokenAsync(string authToken, CancellationToken cancellationToken = default);

    Task ClearAuthenticationTokenAsync(CancellationToken cancellationToken = default);

    Task<FactoryServerResponseContent<HealthCheckData>> HealthCheckAsync(string? clientCustomData, CancellationToken cancellationToken = default);

    Task<FactoryServerResponseContent<LoginData>> PasswordlessLoginAsync(FactoryServerPrivilegeLevel minimumPrivilegeLevel, CancellationToken cancellationToken = default);

    Task<FactoryServerResponseContent<LoginData>> PasswordLoginAsync(FactoryServerPrivilegeLevel minimumPrivilegeLevel, ReadOnlyMemory<char> password, CancellationToken cancellationToken = default);

    Task<FactoryServerError?> VerifyAuthenticationTokenAsync(CancellationToken cancellationToken = default);

    Task<FactoryServerResponseContent<QueryServerStateData>> QueryServerStateAsync(CancellationToken cancellationToken = default);

    Task<FactoryServerResponseContent<GetServerOptionsData>> GetServerOptionsAsync(CancellationToken cancellationToken = default);

    Task<FactoryServerResponseContent<GetAdvancedGameSettingsData>> GetAdvancedGameSettingsAsync(CancellationToken cancellationToken = default);

    Task<FactoryServerError?> ApplyAdvancedGameSettingsAsync(Dictionary<string, string> appliedAdvancedGameSettings, CancellationToken cancellationToken = default);

    Task<FactoryServerResponseContent<LoginData>> ClaimServerAsync(string serverName, ReadOnlyMemory<char> adminPassword, CancellationToken cancellationToken = default);

    Task<FactoryServerError?> RenameServerAsync(string serverName, CancellationToken cancellationToken = default);

    Task<FactoryServerError?> SetClientPasswordAsync(ReadOnlyMemory<char>? password, CancellationToken cancellationToken = default);

    Task<FactoryServerError?> SetAdminPasswordAsync(ReadOnlyMemory<char> password, string authenticationToken, CancellationToken cancellationToken = default);

    Task<FactoryServerError?> SetAutoLoadSessionNameAsync(string sessionName, CancellationToken cancellationToken = default);

    Task<FactoryServerResponseContent<RunCommandData>> RunCommandAsync(string command, CancellationToken cancellationToken = default);

    Task<FactoryServerError?> ShutdownAsync(CancellationToken cancellationToken = default);

    Task<FactoryServerError?> ApplyServerOptions(Dictionary<string, string> updatedServerOptions, CancellationToken cancellationToken = default);

    Task<FactoryServerError?> CreateNewGameAsync(ServerNewGameData newGameData, CancellationToken cancellationToken = default);

    Task<FactoryServerError?> SaveGameAsync(string saveName, CancellationToken cancellationToken = default);

    Task<FactoryServerError?> DeleteSaveFileAsync(string saveName, CancellationToken cancellationToken = default);

    Task<FactoryServerError?> DeleteSaveSessionAsync(string sessionName, CancellationToken cancellationToken = default);

    Task<FactoryServerResponseContent<EnumerateSessionsData>> EnumerateSessionsAsync(CancellationToken cancellationToken = default);

    Task<FactoryServerError?> LoadGameAsync(string saveName, bool enableAdvancedGameSettings, CancellationToken cancellationToken = default);

    Task<FactoryServerError?> UploadSaveGameAsync(string saveName, bool loadSaveGame, bool enableAdvancedGameSettings, Stream saveGameFile, CancellationToken cancellationToken = default);

    Task<FactoryServerResponseContent<DownloadSaveGameData>> DownloadSaveGameAsync(string saveName, CancellationToken cancellationToken = default);
}