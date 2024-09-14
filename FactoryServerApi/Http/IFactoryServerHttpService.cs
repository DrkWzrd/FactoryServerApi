using FactoryServerApi.Http.Responses;

namespace FactoryServerApi.Http;

public interface IFactoryServerHttpService
{
    Task<(HealthCheckResponseData? Result, FactoryServerError? Error)> HealthCheckAsync(string? clientCustomData, FactoryServerConnectionInfo connectionInfo);

    Task<FactoryServerError?> VerifyAuthenticationToken(FactoryServerConnectionInfo connectionInfo);

    Task<(LoginResponseData? Result, FactoryServerError? Error)> PasswordlessLoginAsync(FactoryServerPrivilegeLevel minimumPrivilegeLevel, FactoryServerConnectionInfo connectionInfo);

    Task<(LoginResponseData? Result, FactoryServerError? Error)> PasswordLoginAsync(FactoryServerPrivilegeLevel minimumPrivilegeLevel, string password, FactoryServerConnectionInfo connectionInfo);

    Task<(QueryServerStateResponseData? Result, FactoryServerError? Error)> QueryServerStateAsync(FactoryServerConnectionInfo connectionInfo);

    Task<(GetServerOptionsResponseData? Result, FactoryServerError? Error)> GetServerOptionsAsync(FactoryServerConnectionInfo connectionInfo);

    Task<(GetAdvancedGameSettingsResponseData? Result, FactoryServerError? Error)> GetAdvancedGameSettingsAsync(FactoryServerConnectionInfo connectionInfo);

    Task<FactoryServerError?> ApplyAdvancedGameSettingsAsync(Dictionary<string, string> appliedAdvancedGameSettings, FactoryServerConnectionInfo connectionInfo);

    Task<(LoginResponseData? Result, FactoryServerError? Error)> ClaimServerAsync(string serverName, string adminPassword, FactoryServerConnectionInfo connectionInfo);

    Task<FactoryServerError?> RenameServerAsync(string serverName, FactoryServerConnectionInfo connectionInfo);

    Task<FactoryServerError?> SetClientPasswordAsync(string password, FactoryServerConnectionInfo connectionInfo);

    Task<FactoryServerError?> SetAdminPasswordAsync(string password, string authenticationToken, FactoryServerConnectionInfo connectionInfo);

    Task<FactoryServerError?> SetAutoLoadSessionNameAsync(string sessionName, FactoryServerConnectionInfo connectionInfo);

    Task<(RunCommandResponseData? Result, FactoryServerError? Error)> RunCommandAsync(string command, FactoryServerConnectionInfo connectionInfo);

    Task<FactoryServerError?> ShutdownAsync(FactoryServerConnectionInfo connectionInfo);

    Task<FactoryServerError?> ApplyServerOptions(Dictionary<string, string> updatedServerOptions, FactoryServerConnectionInfo connectionInfo);

    Task<FactoryServerError?> CreateNewGameAsync(ServerNewGameData newGameData, FactoryServerConnectionInfo connectionInfo);

    Task<FactoryServerError?> SaveGameAsync(string saveName, FactoryServerConnectionInfo connectionInfo);

    Task<FactoryServerError?> DeleteSaveFileAsync(string saveName, FactoryServerConnectionInfo connectionInfo);

    Task<FactoryServerError?> DeleteSaveSessionAsync(string sessionName, FactoryServerConnectionInfo connectionInfo);

    Task<(EnumerateSessionsResponseData? Result, FactoryServerError? Error)> EnumerateSessionsAsync(FactoryServerConnectionInfo connectionInfo);

    Task<FactoryServerError?> LoadGameAsync(string saveName, bool enableAdvancedGameSettings, FactoryServerConnectionInfo connectionInfo);

    Task<FactoryServerError?> UploadSaveGameAsync(string saveName, bool loadSaveGame, bool enableAdvancedGameSettings, Stream saveGameFile, FactoryServerConnectionInfo connectionInfo);

    Task<(DownloadSaveGameResponseData? Result, FactoryServerError? Error)> DownloadSaveGameAsync(string saveName, FactoryServerConnectionInfo connectionInfo);
}