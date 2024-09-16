using FactoryServerApi.Http.Requests.Contents;
using FactoryServerApi.Http.Responses;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;

namespace FactoryServerApi.Http;

internal class FactoryServerHttpService : IFactoryServerHttpService
{
    private record DataWrapper<T>(T Data);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _settingsApiPath;
    private readonly SemaphoreSlim _connectionInfoSemaphore = new(1, 1);
    private FactoryServerConnectionInfo? _connectionInfo;

    public async Task<FactoryServerConnectionInfo?> GetConnectionInfoAsync()
    {
        await _connectionInfoSemaphore.WaitAsync();
        try
        {
            return _connectionInfo;
        }
        finally
        {
            _connectionInfoSemaphore.Release();
        }
    }

    public async Task SetConnectionInfoAsync(FactoryServerConnectionInfo? value)
    {
        await _connectionInfoSemaphore.WaitAsync();
        try
        {
            _connectionInfo = value;
        }
        finally
        {
            _connectionInfoSemaphore.Release();
        }
    }

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
        var innerConnectionInfoUsed = connectionInfo is null;
        if (innerConnectionInfoUsed)
            await _connectionInfoSemaphore.WaitAsync();

        connectionInfo ??= _connectionInfo ?? throw new InvalidOperationException("The connection info must be set in the service or passed as parameter");

        try
        {
            var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
            SetupHttpClient(httpClient, connectionInfo);
            var apiPath = connectionInfo.ApiPath ?? _settingsApiPath;
            var response = await httpClient.PostAsync(apiPath, content);
            return await HandleResponseAsync<TResult>(response);
        }
        finally
        {
            if (innerConnectionInfoUsed)
                _connectionInfoSemaphore.Release();
        }

    }

    private async Task<FactoryServerError?> ExecuteVoidRequestAsync(HttpContent content, FactoryServerConnectionInfo? connectionInfo = null)
    {
        var innerConnectionInfoUsed = connectionInfo is null;
        if (innerConnectionInfoUsed)
            await _connectionInfoSemaphore.WaitAsync();

        connectionInfo ??= _connectionInfo ?? throw new InvalidOperationException("The connection info must be set in the service or passed as parameter");
        try
        {
            var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
            SetupHttpClient(httpClient, connectionInfo);
            var apiPath = connectionInfo.ApiPath ?? _settingsApiPath;
            var response = await httpClient.PostAsync(apiPath, content);
            return await HandleVoidResponseAsync(response);

        }
        finally
        {
            if (innerConnectionInfoUsed)
                _connectionInfoSemaphore.Release();
        }
    }

    private static async Task<(TResult? Result, FactoryServerError? Error)> HandleResponseAsync<TResult>(HttpResponseMessage response)
        where TResult : class
    {
        return typeof(TResult) == typeof(Stream)
            ? await HandleStreamResponseAsync<TResult>(response)
            : await HandleJsonResponseAsync<TResult>(response);
    }

    private static async Task<(TResult? Result, FactoryServerError? Error)> HandleJsonResponseAsync<TResult>(HttpResponseMessage response)
        where TResult : class
    {
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            return (null, null);

        if (response.Content.Headers.ContentType?.MediaType != MediaTypeNames.Application.Json)
            throw new InvalidOperationException();

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

    private static async Task<(TResult? Result, FactoryServerError? Error)> HandleStreamResponseAsync<TResult>(HttpResponseMessage response)
        where TResult : class
    {
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            return (null, null);

        if (response.IsSuccessStatusCode)
        {
            if (response.Content.Headers.ContentType?.MediaType != MediaTypeNames.Application.Octet)
                throw new InvalidOperationException();

            var responseContent = await response.Content.ReadAsStreamAsync();
            if (responseContent is TResult tResult)
                return (tResult, null);

            throw new InvalidOperationException();
        }
        else
        {
            return await HandleJsonResponseAsync<TResult>(response);
        }

    }

    private static async Task<FactoryServerError?> HandleVoidResponseAsync(HttpResponseMessage response)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            return null;

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