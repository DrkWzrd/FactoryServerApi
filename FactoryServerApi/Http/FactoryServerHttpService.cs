using FactoryServerApi.Http.Requests.Contents;
using FactoryServerApi.Http.Responses;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FactoryServerApi.Http;

internal class FactoryServerHttpService : IFactoryServerHttpService
{
    private record DataWrapper<T>(T Data);

    private readonly IHttpClientFactory _httpClientFactory;

    public FactoryServerHttpService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<(HealthCheckResponseData? Result, FactoryServerError? Error)> HealthCheckAsync(string? clientCustomData, FactoryServerConnectionInfo connectionInfo)
    {
        var content = new HealthCheckContent(clientCustomData);
        var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
        SetupHttpClient(httpClient, connectionInfo);
        var response = await httpClient.PostAsync("api/v1/", content);
        return await HandleResponseAsync<HealthCheckResponseData>(response);
    }

    public async Task<FactoryServerError?> VerifyAuthenticationToken(FactoryServerConnectionInfo connectionInfo)
    {
        var content = new VerifyAuthenticationTokenContent();
        var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
        SetupHttpClient(httpClient, connectionInfo);
        var response = await httpClient.PostAsync("api/v1/", content);
        return await HandleVoidResponseAsync(response);
    }

    public async Task<(LoginResponseData? Result, FactoryServerError? Error)> PasswordlessLoginAsync(FactoryServerPrivilegeLevel minimumPrivilegeLevel, FactoryServerConnectionInfo connectionInfo)
    {
        var content = new PasswordlessLoginContent(minimumPrivilegeLevel);
        var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
        SetupHttpClient(httpClient, connectionInfo);
        var response = await httpClient.PostAsync("api/v1/", content);
        return await HandleResponseAsync<LoginResponseData>(response);
    }

    public async Task<(LoginResponseData? Result, FactoryServerError? Error)> PasswordLoginAsync(FactoryServerPrivilegeLevel minimumPrivilegeLevel, string password, FactoryServerConnectionInfo connectionInfo)
    {
        var content = new PasswordLoginContent(minimumPrivilegeLevel, password);
        var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
        SetupHttpClient(httpClient, connectionInfo);
        var response = await httpClient.PostAsync("api/v1/", content);
        return await HandleResponseAsync<LoginResponseData>(response);
    }

    public async Task<(QueryServerStateResponseData? Result, FactoryServerError? Error)> QueryServerStateAsync(FactoryServerConnectionInfo connectionInfo)
    {
        var content = new QueryServerStateContent();
        var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
        SetupHttpClient(httpClient, connectionInfo);
        var response = await httpClient.PostAsync("api/v1/", content);
        return await HandleResponseAsync<QueryServerStateResponseData>(response);
    }

    public async Task<(GetServerOptionsResponseData? Result, FactoryServerError? Error)> GetServerOptionsAsync(FactoryServerConnectionInfo connectionInfo)
    {
        var content = new GetServerOptionsContent();
        var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
        SetupHttpClient(httpClient, connectionInfo);
        var response = await httpClient.PostAsync("api/v1/", content);
        return await HandleResponseAsync<GetServerOptionsResponseData>(response);
    }

    public async Task<(GetAdvancedGameSettingsResponseData? Result, FactoryServerError? Error)> GetAdvancedGameSettingsAsync(FactoryServerConnectionInfo connectionInfo)
    {
        var content = new GetAdvancedGameSettingsContent();
        var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
        SetupHttpClient(httpClient, connectionInfo);
        var response = await httpClient.PostAsync("api/v1/", content);
        return await HandleResponseAsync<GetAdvancedGameSettingsResponseData>(response);
    }

    public async Task<(LoginResponseData? Result, FactoryServerError? Error)> ClaimServerAsync(string serverName, string adminPassword, FactoryServerConnectionInfo connectionInfo)
    {
        var content = new ClaimServerContent(serverName, adminPassword);
        var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
        SetupHttpClient(httpClient, connectionInfo);
        var response = await httpClient.PostAsync("api/v1/", content);
        return await HandleResponseAsync<LoginResponseData>(response);
    }

    public async Task<(RunCommandResponseData? Result, FactoryServerError? Error)> RunCommandAsync(string command, FactoryServerConnectionInfo connectionInfo)
    {
        var content = new RunCommandContent(command);
        var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
        SetupHttpClient(httpClient, connectionInfo);
        var response = await httpClient.PostAsync("api/v1/", content);
        return await HandleResponseAsync<RunCommandResponseData>(response);
    }

    public async Task<(EnumerateSessionsResponseData? Result, FactoryServerError? Error)> EnumerateSessionsAsync(FactoryServerConnectionInfo connectionInfo)
    {
        var content = new EnumerateSessionsContent();
        var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
        SetupHttpClient(httpClient, connectionInfo);
        var response = await httpClient.PostAsync("api/v1/", content);
        return await HandleResponseAsync<EnumerateSessionsResponseData>(response);
    }

    public async Task<(DownloadSaveGameResponseData? Result, FactoryServerError? Error)> DownloadSaveGameAsync(string saveName, FactoryServerConnectionInfo connectionInfo)
    {
        var content = new DownloadSaveGameContent(saveName);
        var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
        SetupHttpClient(httpClient, connectionInfo);
        var response = await httpClient.PostAsync("api/v1/", content);

        if (response.IsSuccessStatusCode)
        {
            var responseStream = await response.Content.ReadAsStreamAsync();
            return (new DownloadSaveGameResponseData(responseStream), null);
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var error = JsonSerializer.Deserialize<FactoryServerError>(responseContent, FactoryServerContent.FactoryServerJsonOptions);
        return (null, error);
    }

    public async Task<FactoryServerError?> ApplyAdvancedGameSettingsAsync(Dictionary<string, string> appliedAdvancedGameSettings, FactoryServerConnectionInfo connectionInfo)
    {
        var content = new ApplyAdvancedGameSettingsContent(appliedAdvancedGameSettings);
        var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
        SetupHttpClient(httpClient, connectionInfo);
        var response = await httpClient.PostAsync("api/v1/", content);
        return await HandleVoidResponseAsync(response);
    }

    public async Task<FactoryServerError?> RenameServerAsync(string serverName, FactoryServerConnectionInfo connectionInfo)
    {
        var content = new RenameServerContent(serverName);
        var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
        SetupHttpClient(httpClient, connectionInfo);
        var response = await httpClient.PostAsync("api/v1/", content);
        return await HandleVoidResponseAsync(response);
    }

    public async Task<FactoryServerError?> SetClientPasswordAsync(string password, FactoryServerConnectionInfo connectionInfo)
    {
        var content = new SetClientPasswordContent(password);
        var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
        SetupHttpClient(httpClient, connectionInfo);
        var response = await httpClient.PostAsync("api/v1/", content);
        return await HandleVoidResponseAsync(response);
    }

    public async Task<FactoryServerError?> SetAdminPasswordAsync(string password, string authenticationToken, FactoryServerConnectionInfo connectionInfo)
    {
        var content = new SetAdminPasswordContent(password, authenticationToken);
        var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
        SetupHttpClient(httpClient, connectionInfo);
        var response = await httpClient.PostAsync("api/v1/", content);
        return await HandleVoidResponseAsync(response);
    }

    public async Task<FactoryServerError?> SetAutoLoadSessionNameAsync(string sessionName, FactoryServerConnectionInfo connectionInfo)
    {
        var content = new SetAutoLoadSessionNameContent(sessionName);
        var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
        SetupHttpClient(httpClient, connectionInfo);
        var response = await httpClient.PostAsync("api/v1/", content);
        return await HandleVoidResponseAsync(response);
    }

    public async Task<FactoryServerError?> ShutdownAsync(FactoryServerConnectionInfo connectionInfo)
    {
        var content = new ShutdownContent();
        var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
        SetupHttpClient(httpClient, connectionInfo);
        var response = await httpClient.PostAsync("api/v1/", content);
        return await HandleVoidResponseAsync(response);
    }

    public async Task<FactoryServerError?> ApplyServerOptions(Dictionary<string, string> updatedServerOptions, FactoryServerConnectionInfo connectionInfo)
    {
        var content = new ApplyServerOptionsContent(updatedServerOptions);
        var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
        SetupHttpClient(httpClient, connectionInfo);
        var response = await httpClient.PostAsync("api/v1/", content);
        return await HandleVoidResponseAsync(response);
    }

    public async Task<FactoryServerError?> CreateNewGameAsync(ServerNewGameData newGameData, FactoryServerConnectionInfo connectionInfo)
    {
        var content = new CreateNewGameContent(newGameData);
        var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
        SetupHttpClient(httpClient, connectionInfo);
        var response = await httpClient.PostAsync("api/v1/", content);
        return await HandleVoidResponseAsync(response);
    }

    public async Task<FactoryServerError?> SaveGameAsync(string saveName, FactoryServerConnectionInfo connectionInfo)
    {
        var content = new SaveGameContent(saveName);
        var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
        SetupHttpClient(httpClient, connectionInfo);
        var response = await httpClient.PostAsync("api/v1/", content);
        return await HandleVoidResponseAsync(response);
    }

    public async Task<FactoryServerError?> DeleteSaveFileAsync(string saveName, FactoryServerConnectionInfo connectionInfo)
    {
        var content = new DeleteSaveFileContent(saveName);
        var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
        SetupHttpClient(httpClient, connectionInfo);
        var response = await httpClient.PostAsync("api/v1/", content);
        return await HandleVoidResponseAsync(response);
    }

    public async Task<FactoryServerError?> DeleteSaveSessionAsync(string sessionName, FactoryServerConnectionInfo connectionInfo)
    {
        var content = new DeleteSaveSessionContent(sessionName);
        var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
        SetupHttpClient(httpClient, connectionInfo);
        var response = await httpClient.PostAsync("api/v1/", content);
        return await HandleVoidResponseAsync(response);
    }

    public async Task<FactoryServerError?> LoadGameAsync(string saveName, bool enableAdvancedGameSettings, FactoryServerConnectionInfo connectionInfo)
    {
        var content = new LoadGameContent(saveName, enableAdvancedGameSettings);
        var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
        SetupHttpClient(httpClient, connectionInfo);
        var response = await httpClient.PostAsync("api/v1/", content);
        return await HandleVoidResponseAsync(response);
    }

    public async Task<FactoryServerError?> UploadSaveGameAsync(string saveName, bool loadSaveGame, bool enableAdvancedGameSettings, Stream saveGameFile, FactoryServerConnectionInfo connectionInfo)
    {
        var content = new UploadSaveGameContent(saveName, loadSaveGame, enableAdvancedGameSettings, saveGameFile);
        var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
        SetupHttpClient(httpClient, connectionInfo);
        var response = await httpClient.PostAsync("api/v1/", content);
        return await HandleVoidResponseAsync(response);
    }

    // Helper method to handle the repeated logic for response success/error handling
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

    private static void SetupHttpClient(HttpClient hClient, FactoryServerConnectionInfo fInfo)
    {
        hClient.BaseAddress = fInfo.GetUrl();
        if (fInfo.AuthenticationToken is null)
            return;
        hClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fInfo.AuthenticationToken);
    }

}