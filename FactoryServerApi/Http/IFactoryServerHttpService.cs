using FactoryServerApi.Http.Responses;

namespace FactoryServerApi.Http;

public interface IFactoryServerHttpService
{
    Task<(HealthCheckResponseData? Result, FactoryServerError? Error)> HealthCheckAsync(string? clientCustomData, FactoryServerConnectionInfo? connectionInfo = null);

    Task<FactoryServerError?> VerifyAuthenticationToken(FactoryServerConnectionInfo? connectionInfo = null);

    Task<(LoginResponseData? Result, FactoryServerError? Error)> PasswordlessLoginAsync(FactoryServerPrivilegeLevel minimumPrivilegeLevel, FactoryServerConnectionInfo? connectionInfo = null);

    Task<(LoginResponseData? Result, FactoryServerError? Error)> PasswordLoginAsync(FactoryServerPrivilegeLevel minimumPrivilegeLevel, string password, FactoryServerConnectionInfo? connectionInfo = null);

    Task<(QueryServerStateResponseData? Result, FactoryServerError? Error)> QueryServerStateAsync(FactoryServerConnectionInfo? connectionInfo = null);

    Task<(GetServerOptionsResponseData? Result, FactoryServerError? Error)> GetServerOptionsAsync(FactoryServerConnectionInfo? connectionInfo = null);

    Task<(GetAdvancedGameSettingsResponseData? Result, FactoryServerError? Error)> GetAdvancedGameSettingsAsync(FactoryServerConnectionInfo? connectionInfo = null);

    Task<FactoryServerError?> ApplyAdvancedGameSettingsAsync(Dictionary<string, string> appliedAdvancedGameSettings, FactoryServerConnectionInfo? connectionInfo = null);

    Task<(LoginResponseData? Result, FactoryServerError? Error)> ClaimServerAsync(string serverName, string adminPassword, FactoryServerConnectionInfo? connectionInfo = null);

    Task<FactoryServerError?> RenameServerAsync(string serverName, FactoryServerConnectionInfo? connectionInfo = null);

    Task<FactoryServerError?> SetClientPasswordAsync(string password, FactoryServerConnectionInfo? connectionInfo = null);

    Task<FactoryServerError?> SetAdminPasswordAsync(string password, string authenticationToken, FactoryServerConnectionInfo? connectionInfo = null);

    Task<FactoryServerError?> SetAutoLoadSessionNameAsync(string sessionName, FactoryServerConnectionInfo? connectionInfo = null);

    Task<(RunCommandResponseData? Result, FactoryServerError? Error)> RunCommandAsync(string command, FactoryServerConnectionInfo? connectionInfo = null);

    Task<FactoryServerError?> ShutdownAsync(FactoryServerConnectionInfo? connectionInfo = null);

    Task<FactoryServerError?> ApplyServerOptions(Dictionary<string, string> updatedServerOptions, FactoryServerConnectionInfo? connectionInfo = null);

    Task<FactoryServerError?> CreateNewGameAsync(ServerNewGameData newGameData, FactoryServerConnectionInfo? connectionInfo = null);

    Task<FactoryServerError?> SaveGameAsync(string saveName, FactoryServerConnectionInfo? connectionInfo = null);

    Task<FactoryServerError?> DeleteSaveFileAsync(string saveName, FactoryServerConnectionInfo? connectionInfo = null);

    Task<FactoryServerError?> DeleteSaveSessionAsync(string sessionName, FactoryServerConnectionInfo? connectionInfo = null);

    Task<(EnumerateSessionsResponseData? Result, FactoryServerError? Error)> EnumerateSessionsAsync(FactoryServerConnectionInfo? connectionInfo = null);

    Task<FactoryServerError?> LoadGameAsync(string saveName, bool enableAdvancedGameSettings, FactoryServerConnectionInfo? connectionInfo = null);

    Task<FactoryServerError?> UploadSaveGameAsync(string saveName, bool loadSaveGame, bool enableAdvancedGameSettings, Stream saveGameFile, FactoryServerConnectionInfo? connectionInfo = null);

    Task<(DownloadSaveGameResponseData? Result, FactoryServerError? Error)> DownloadSaveGameAsync(string saveName, FactoryServerConnectionInfo? connectionInfo = null);
}