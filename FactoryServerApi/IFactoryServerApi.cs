using FactoryServerApi.Http;
using FactoryServerApi.Http.Responses;

namespace FactoryServerApi;

public interface IFactoryServerApi : IFactoryServerHttpService
{
    //This occurs automaticly to FactoryServerInfo
    //Task<(HealthCheckResponseData? Result, FactoryServerError? Error)> HealthCheckAsync(string? clientCustomData)
    //    => HealthCheckAsync(clientCustomData, null);
    //Task<(QueryServerStateResponseData? Result, FactoryServerError? Error)> QueryServerStateAsync()
    //    => QueryServerStateAsync(null);
    //Task<(GetServerOptionsResponseData? Result, FactoryServerError? Error)> GetServerOptionsAsync()
    //    => GetServerOptionsAsync(null);
    //Task<(GetAdvancedGameSettingsResponseData? Result, FactoryServerError? Error)> GetAdvancedGameSettingsAsync()
    //    => GetAdvancedGameSettingsAsync(null);
    //Task<(EnumerateSessionsResponseData? Result, FactoryServerError? Error)> EnumerateSessionsAsync()
    //    => EnumerateSessionsAsync(null);

    Task<FactoryServerError?> VerifyAuthenticationToken()
        => VerifyAuthenticationToken(null);

    //You have to be authenticated to use this interface
    //Task<(LoginResponseData? Result, FactoryServerError? Error)> PasswordlessLoginAsync(FactoryServerPrivilegeLevel minimumPrivilegeLevel)
    //    => PasswordlessLoginAsync(minimumPrivilegeLevel, null);
    //Task<(LoginResponseData? Result, FactoryServerError? Error)> PasswordLoginAsync(FactoryServerPrivilegeLevel minimumPrivilegeLevel, string password)
    //    => PasswordLoginAsync(minimumPrivilegeLevel, password, null);
    //The server must be claimed for use this interface
    //Task<(LoginResponseData? Result, FactoryServerError? Error)> ClaimServerAsync(string serverName, string adminPassword)
    //    => ClaimServerAsync(serverName, adminPassword, null);

    Task<FactoryServerError?> ApplyAdvancedGameSettingsAsync(Dictionary<string, string> appliedAdvancedGameSettings)
        => ApplyAdvancedGameSettingsAsync(appliedAdvancedGameSettings, null);

    Task<FactoryServerError?> RenameServerAsync(string serverName)
        => RenameServerAsync(serverName, null);

    Task<FactoryServerError?> SetClientPasswordAsync(string password)
        => SetClientPasswordAsync(password, null);

    Task<FactoryServerError?> SetAdminPasswordAsync(string password, string authenticationToken)
        => SetAdminPasswordAsync(password, authenticationToken, null);

    Task<FactoryServerError?> SetAutoLoadSessionNameAsync(string sessionName)
        => SetAutoLoadSessionNameAsync(sessionName, null);

    Task<(RunCommandResponseData? Result, FactoryServerError? Error)> RunCommandAsync(string command)
        => RunCommandAsync(command, null);

    Task<FactoryServerError?> ShutdownAsync()
        => ShutdownAsync(null);

    Task<FactoryServerError?> ApplyServerOptions(Dictionary<string, string> updatedServerOptions)
        => ApplyServerOptions(updatedServerOptions, null);

    Task<FactoryServerError?> CreateNewGameAsync(ServerNewGameData newGameData)
        => CreateNewGameAsync(newGameData, null);

    Task<FactoryServerError?> SaveGameAsync(string saveName)
        => SaveGameAsync(saveName, null);

    Task<FactoryServerError?> DeleteSaveFileAsync(string saveName)
        => DeleteSaveFileAsync(saveName, null);

    Task<FactoryServerError?> DeleteSaveSessionAsync(string sessionName)
        => DeleteSaveSessionAsync(sessionName, null);

    Task<FactoryServerError?> LoadGameAsync(string saveName, bool enableAdvancedGameSettings)
        => LoadGameAsync(saveName, enableAdvancedGameSettings, null);

    Task<FactoryServerError?> UploadSaveGameAsync(string saveName, bool loadSaveGame, bool enableAdvancedGameSettings, Stream saveGameFile)
        => UploadSaveGameAsync(saveName, loadSaveGame, enableAdvancedGameSettings, saveGameFile, null);

    Task<(DownloadSaveGameResponseData? Result, FactoryServerError? Error)> DownloadSaveGameAsync(string saveName)
        => DownloadSaveGameAsync(saveName, null);
}
