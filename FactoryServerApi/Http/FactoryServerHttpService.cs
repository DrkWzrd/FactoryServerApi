using FactoryServerApi.Http.Requests.Contents;
using FactoryServerApi.Http.Responses;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FactoryServerApi.Http;

internal class FactoryServerHttpService : IFactoryServerHttpService
{
    private record DataWrapper<T>(T Data);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _settingsApiPath;

    public FactoryServerConnectionInfo? ConnectionInfo { get; set; }

    public FactoryServerHttpService(IOptions<HttpOptions> options, IHttpClientFactory httpClientFactory)
    {
        _settingsApiPath = options.Value.ApiPath;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<(HealthCheckResponseData? Result, FactoryServerError? Error)> HealthCheckAsync(string? clientCustomData, FactoryServerConnectionInfo? connectionInfo = null)
    {
        var content = new HealthCheckContent(clientCustomData);
        return await ExecuteRequestAsync<HealthCheckResponseData>(content, connectionInfo);
    }

    public async Task<FactoryServerError?> VerifyAuthenticationToken(FactoryServerConnectionInfo? connectionInfo = null)
    {
        var content = new VerifyAuthenticationTokenContent();
        return await ExecuteVoidRequestAsync(content, connectionInfo);
    }

    public async Task<(LoginResponseData? Result, FactoryServerError? Error)> PasswordlessLoginAsync(FactoryServerPrivilegeLevel minimumPrivilegeLevel, FactoryServerConnectionInfo? connectionInfo = null)
    {
        var content = new PasswordlessLoginContent(minimumPrivilegeLevel);
        return await ExecuteRequestAsync<LoginResponseData>(content, connectionInfo);
    }

    public async Task<(LoginResponseData? Result, FactoryServerError? Error)> PasswordLoginAsync(FactoryServerPrivilegeLevel minimumPrivilegeLevel, string password, FactoryServerConnectionInfo? connectionInfo = null)
    {
        var content = new PasswordLoginContent(minimumPrivilegeLevel, password);
        return await ExecuteRequestAsync<LoginResponseData>(content, connectionInfo);
    }

    public async Task<(QueryServerStateResponseData? Result, FactoryServerError? Error)> QueryServerStateAsync(FactoryServerConnectionInfo? connectionInfo = null)
    {
        var content = new QueryServerStateContent();
        return await ExecuteRequestAsync<QueryServerStateResponseData>(content, connectionInfo);
    }

    public async Task<(GetServerOptionsResponseData? Result, FactoryServerError? Error)> GetServerOptionsAsync(FactoryServerConnectionInfo? connectionInfo = null)
    {
        var content = new GetServerOptionsContent();
        return await ExecuteRequestAsync<GetServerOptionsResponseData>(content, connectionInfo);
    }

    public async Task<(GetAdvancedGameSettingsResponseData? Result, FactoryServerError? Error)> GetAdvancedGameSettingsAsync(FactoryServerConnectionInfo? connectionInfo = null)
    {
        var content = new GetAdvancedGameSettingsContent();
        return await ExecuteRequestAsync<GetAdvancedGameSettingsResponseData>(content, connectionInfo);
    }

    public async Task<(LoginResponseData? Result, FactoryServerError? Error)> ClaimServerAsync(string serverName, string adminPassword, FactoryServerConnectionInfo? connectionInfo = null)
    {
        var content = new ClaimServerContent(serverName, adminPassword);
        return await ExecuteRequestAsync<LoginResponseData>(content, connectionInfo);
    }

    public async Task<(RunCommandResponseData? Result, FactoryServerError? Error)> RunCommandAsync(string command, FactoryServerConnectionInfo? connectionInfo = null)
    {
        var content = new RunCommandContent(command);
        return await ExecuteRequestAsync<RunCommandResponseData>(content, connectionInfo);
    }

    public async Task<(EnumerateSessionsResponseData? Result, FactoryServerError? Error)> EnumerateSessionsAsync(FactoryServerConnectionInfo? connectionInfo = null)
    {
        var content = new EnumerateSessionsContent();
        return await ExecuteRequestAsync<EnumerateSessionsResponseData>(content, connectionInfo);
    }

    public async Task<(DownloadSaveGameResponseData? Result, FactoryServerError? Error)> DownloadSaveGameAsync(string saveName, FactoryServerConnectionInfo? connectionInfo = null)
    {
        var content = new DownloadSaveGameContent(saveName);
        var (response, error) = await ExecuteRequestAsync<Stream>(content, connectionInfo);
        return response is not null
            ? (new DownloadSaveGameResponseData(response), null)
            : (null, error);
    }

    public async Task<FactoryServerError?> ApplyAdvancedGameSettingsAsync(Dictionary<string, string> appliedAdvancedGameSettings, FactoryServerConnectionInfo? connectionInfo = null)
    {
        var content = new ApplyAdvancedGameSettingsContent(appliedAdvancedGameSettings);
        return await ExecuteVoidRequestAsync(content, connectionInfo);
    }

    public async Task<FactoryServerError?> RenameServerAsync(string serverName, FactoryServerConnectionInfo? connectionInfo = null)
    {
        var content = new RenameServerContent(serverName);
        return await ExecuteVoidRequestAsync(content, connectionInfo);
    }

    public async Task<FactoryServerError?> SetClientPasswordAsync(string password, FactoryServerConnectionInfo? connectionInfo = null)
    {
        var content = new SetClientPasswordContent(password);
        return await ExecuteVoidRequestAsync(content, connectionInfo);
    }

    public async Task<FactoryServerError?> SetAdminPasswordAsync(string password, string authenticationToken, FactoryServerConnectionInfo? connectionInfo = null)
    {
        var content = new SetAdminPasswordContent(password, authenticationToken);
        return await ExecuteVoidRequestAsync(content, connectionInfo);
    }

    public async Task<FactoryServerError?> SetAutoLoadSessionNameAsync(string sessionName, FactoryServerConnectionInfo? connectionInfo = null)
    {
        var content = new SetAutoLoadSessionNameContent(sessionName);
        return await ExecuteVoidRequestAsync(content, connectionInfo);
    }

    public async Task<FactoryServerError?> ShutdownAsync(FactoryServerConnectionInfo? connectionInfo = null)
    {
        var content = new ShutdownContent();
        return await ExecuteVoidRequestAsync(content, connectionInfo);
    }

    public async Task<FactoryServerError?> ApplyServerOptions(Dictionary<string, string> updatedServerOptions, FactoryServerConnectionInfo? connectionInfo = null)
    {
        var content = new ApplyServerOptionsContent(updatedServerOptions);
        return await ExecuteVoidRequestAsync(content, connectionInfo);
    }

    public async Task<FactoryServerError?> CreateNewGameAsync(ServerNewGameData newGameData, FactoryServerConnectionInfo? connectionInfo = null)
    {
        var content = new CreateNewGameContent(newGameData);
        return await ExecuteVoidRequestAsync(content, connectionInfo);
    }

    public async Task<FactoryServerError?> SaveGameAsync(string saveName, FactoryServerConnectionInfo? connectionInfo = null)
    {
        var content = new SaveGameContent(saveName);
        return await ExecuteVoidRequestAsync(content, connectionInfo);
    }

    public async Task<FactoryServerError?> DeleteSaveFileAsync(string saveName, FactoryServerConnectionInfo? connectionInfo = null)
    {
        var content = new DeleteSaveFileContent(saveName);
        return await ExecuteVoidRequestAsync(content, connectionInfo);
    }

    public async Task<FactoryServerError?> DeleteSaveSessionAsync(string sessionName, FactoryServerConnectionInfo? connectionInfo = null)
    {
        var content = new DeleteSaveSessionContent(sessionName);
        return await ExecuteVoidRequestAsync(content, connectionInfo);
    }

    public async Task<FactoryServerError?> LoadGameAsync(string saveName, bool enableAdvancedGameSettings, FactoryServerConnectionInfo? connectionInfo = null)
    {
        var content = new LoadGameContent(saveName, enableAdvancedGameSettings);
        return await ExecuteVoidRequestAsync(content, connectionInfo);
    }

    public async Task<FactoryServerError?> UploadSaveGameAsync(string saveName, bool loadSaveGame, bool enableAdvancedGameSettings, Stream saveGameFile, FactoryServerConnectionInfo? connectionInfo = null)
    {
        var content = new UploadSaveGameContent(saveName, loadSaveGame, enableAdvancedGameSettings, saveGameFile);
        return await ExecuteVoidRequestAsync(content, connectionInfo);
    }


    private async Task<(TResult? Result, FactoryServerError? Error)> ExecuteRequestAsync<TResult>(HttpContent content, FactoryServerConnectionInfo? connectionInfo = null)
        where TResult : class
    {
        var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
        connectionInfo ??= ConnectionInfo ?? throw new InvalidOperationException("The connection info must be set in the service or passed as parameter");
        SetupHttpClient(httpClient, connectionInfo);
        var apiPath = connectionInfo.ApiPath ?? _settingsApiPath;
        var response = await httpClient.PostAsync(apiPath, content);
        return await HandleResponseAsync<TResult>(response);
    }

    private async Task<FactoryServerError?> ExecuteVoidRequestAsync(HttpContent content, FactoryServerConnectionInfo? connectionInfo = null)
    {
        var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
        connectionInfo ??= ConnectionInfo ?? throw new InvalidOperationException("The connection info must be set in the service or passed as parameter");
        SetupHttpClient(httpClient, connectionInfo);
        var apiPath = connectionInfo.ApiPath ?? _settingsApiPath;
        var response = await httpClient.PostAsync(apiPath, content);
        return await HandleVoidResponseAsync(response);
    }

    private static async Task<(TResult? Result, FactoryServerError? Error)> HandleResponseAsync<TResult>(HttpResponseMessage response)
        where TResult : class
    {
        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var wrappedData = JsonSerializer.Deserialize<DataWrapper<TResult>>(responseContent, FactoryServerContent.FactoryServerJsonOptions)
                ?? throw new InvalidOperationException();

            return (wrappedData.Data, null);
        }

        var error = JsonSerializer.Deserialize<FactoryServerError>(responseContent, FactoryServerContent.FactoryServerJsonOptions);
        return (null, error);
    }

    private static async Task<FactoryServerError?> HandleVoidResponseAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return null;

        var responseContent = await response.Content.ReadAsStringAsync();
        var error = JsonSerializer.Deserialize<FactoryServerError>(responseContent, FactoryServerContent.FactoryServerJsonOptions);
        return error;
    }

    private static void SetupHttpClient(HttpClient hClient, FactoryServerConnectionInfo connectionInfo)
    {
        hClient.BaseAddress = connectionInfo.GetUrl();
        if (connectionInfo.AuthenticationToken is not null)
            hClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", connectionInfo.AuthenticationToken);

        if (connectionInfo.PlayerId is not null)
            hClient.DefaultRequestHeaders.Add("X-FactoryGame-PlayerId", connectionInfo.PlayerId.Value.ToString());
    }

}