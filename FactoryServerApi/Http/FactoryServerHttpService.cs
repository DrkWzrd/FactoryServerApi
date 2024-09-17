using FactoryServerApi.Http.Requests.Contents;
using FactoryServerApi.Http.Responses;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;

namespace FactoryServerApi.Http;

internal class FactoryServerHttpService : IFactoryServerApi
{
    private record DataWrapper<T>(T Data);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _settingsApiPath;
    private readonly SemaphoreSlim _connectionInfoSemaphore = new(1, 1);
    private FactoryServerConnectionInfo? _connectionInfo;

    async Task<FactoryServerConnectionInfo?> IFactoryServerHttpService.GetConnectionInfoAsync()
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

    async Task IFactoryServerHttpService.SetConnectionInfoAsync(FactoryServerConnectionInfo? value)
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

    async Task<(HealthCheckResponseData? Result, FactoryServerError? Error)> IFactoryServerHttpService.HealthCheckAsync(string? clientCustomData, FactoryServerConnectionInfo? connectionInfo)
    {
        var content = new HealthCheckContent(clientCustomData);
        return await ExecuteRequestAsync<HealthCheckResponseData>(content, connectionInfo);
    }

    async Task<FactoryServerError?> IFactoryServerHttpService.VerifyAuthenticationToken(FactoryServerConnectionInfo? connectionInfo)
    {
        var content = new VerifyAuthenticationTokenContent();
        return await ExecuteVoidRequestAsync(content, connectionInfo);
    }

    async Task<(LoginResponseData? Result, FactoryServerError? Error)> IFactoryServerHttpService.PasswordlessLoginAsync(FactoryServerPrivilegeLevel minimumPrivilegeLevel, FactoryServerConnectionInfo? connectionInfo)
    {
        var content = new PasswordlessLoginContent(minimumPrivilegeLevel);
        return await ExecuteRequestAsync<LoginResponseData>(content, connectionInfo);
    }

    async Task<(LoginResponseData? Result, FactoryServerError? Error)> IFactoryServerHttpService.PasswordLoginAsync(FactoryServerPrivilegeLevel minimumPrivilegeLevel, string password, FactoryServerConnectionInfo? connectionInfo)
    {
        var content = new PasswordLoginContent(minimumPrivilegeLevel, password);
        return await ExecuteRequestAsync<LoginResponseData>(content, connectionInfo);
    }

    async Task<(QueryServerStateResponseData? Result, FactoryServerError? Error)> IFactoryServerHttpService.QueryServerStateAsync(FactoryServerConnectionInfo? connectionInfo)
    {
        var content = new QueryServerStateContent();
        return await ExecuteRequestAsync<QueryServerStateResponseData>(content, connectionInfo);
    }

    async Task<(GetServerOptionsResponseData? Result, FactoryServerError? Error)> IFactoryServerHttpService.GetServerOptionsAsync(FactoryServerConnectionInfo? connectionInfo)
    {
        var content = new GetServerOptionsContent();
        return await ExecuteRequestAsync<GetServerOptionsResponseData>(content, connectionInfo);
    }

    async Task<(GetAdvancedGameSettingsResponseData? Result, FactoryServerError? Error)> IFactoryServerHttpService.GetAdvancedGameSettingsAsync(FactoryServerConnectionInfo? connectionInfo)
    {
        var content = new GetAdvancedGameSettingsContent();
        return await ExecuteRequestAsync<GetAdvancedGameSettingsResponseData>(content, connectionInfo);
    }

    async Task<(LoginResponseData? Result, FactoryServerError? Error)> IFactoryServerHttpService.ClaimServerAsync(string serverName, string adminPassword, FactoryServerConnectionInfo? connectionInfo)
    {
        var content = new ClaimServerContent(serverName, adminPassword);
        return await ExecuteRequestAsync<LoginResponseData>(content, connectionInfo);
    }

    async Task<(RunCommandResponseData? Result, FactoryServerError? Error)> IFactoryServerHttpService.RunCommandAsync(string command, FactoryServerConnectionInfo? connectionInfo)
    {
        var content = new RunCommandContent(command);
        return await ExecuteRequestAsync<RunCommandResponseData>(content, connectionInfo);
    }

    async Task<(EnumerateSessionsResponseData? Result, FactoryServerError? Error)> IFactoryServerHttpService.EnumerateSessionsAsync(FactoryServerConnectionInfo? connectionInfo)
    {
        var content = new EnumerateSessionsContent();
        return await ExecuteRequestAsync<EnumerateSessionsResponseData>(content, connectionInfo);
    }

    async Task<(DownloadSaveGameResponseData? Result, FactoryServerError? Error)> IFactoryServerHttpService.DownloadSaveGameAsync(string saveName, FactoryServerConnectionInfo? connectionInfo)
    {
        var content = new DownloadSaveGameContent(saveName);
        var (response, error) = await ExecuteRequestAsync<Stream>(content, connectionInfo);
        return response is not null
            ? (new DownloadSaveGameResponseData(response), null)
            : (null, error);
    }

    async Task<FactoryServerError?> IFactoryServerHttpService.ApplyAdvancedGameSettingsAsync(Dictionary<string, string> appliedAdvancedGameSettings, FactoryServerConnectionInfo? connectionInfo)
    {
        var content = new ApplyAdvancedGameSettingsContent(appliedAdvancedGameSettings);
        return await ExecuteVoidRequestAsync(content, connectionInfo);
    }

    async Task<FactoryServerError?> IFactoryServerHttpService.RenameServerAsync(string serverName, FactoryServerConnectionInfo? connectionInfo)
    {
        var content = new RenameServerContent(serverName);
        return await ExecuteVoidRequestAsync(content, connectionInfo);
    }

    async Task<FactoryServerError?> IFactoryServerHttpService.SetClientPasswordAsync(string password, FactoryServerConnectionInfo? connectionInfo)
    {
        var content = new SetClientPasswordContent(password);
        return await ExecuteVoidRequestAsync(content, connectionInfo);
    }

    async Task<FactoryServerError?> IFactoryServerHttpService.SetAdminPasswordAsync(string password, string authenticationToken, FactoryServerConnectionInfo? connectionInfo)
    {
        var content = new SetAdminPasswordContent(password, authenticationToken);
        return await ExecuteVoidRequestAsync(content, connectionInfo);
    }

    async Task<FactoryServerError?> IFactoryServerHttpService.SetAutoLoadSessionNameAsync(string sessionName, FactoryServerConnectionInfo? connectionInfo)
    {
        var content = new SetAutoLoadSessionNameContent(sessionName);
        return await ExecuteVoidRequestAsync(content, connectionInfo);
    }

    async Task<FactoryServerError?> IFactoryServerHttpService.ShutdownAsync(FactoryServerConnectionInfo? connectionInfo)
    {
        var content = new ShutdownContent();
        return await ExecuteVoidRequestAsync(content, connectionInfo);
    }

    async Task<FactoryServerError?> IFactoryServerHttpService.ApplyServerOptions(Dictionary<string, string> updatedServerOptions, FactoryServerConnectionInfo? connectionInfo)
    {
        var content = new ApplyServerOptionsContent(updatedServerOptions);
        return await ExecuteVoidRequestAsync(content, connectionInfo);
    }

    async Task<FactoryServerError?> IFactoryServerHttpService.CreateNewGameAsync(ServerNewGameData newGameData, FactoryServerConnectionInfo? connectionInfo)
    {
        var content = new CreateNewGameContent(newGameData);
        return await ExecuteVoidRequestAsync(content, connectionInfo);
    }

    async Task<FactoryServerError?> IFactoryServerHttpService.SaveGameAsync(string saveName, FactoryServerConnectionInfo? connectionInfo)
    {
        var content = new SaveGameContent(saveName);
        return await ExecuteVoidRequestAsync(content, connectionInfo);
    }

    async Task<FactoryServerError?> IFactoryServerHttpService.DeleteSaveFileAsync(string saveName, FactoryServerConnectionInfo? connectionInfo)
    {
        var content = new DeleteSaveFileContent(saveName);
        return await ExecuteVoidRequestAsync(content, connectionInfo);
    }

    async Task<FactoryServerError?> IFactoryServerHttpService.DeleteSaveSessionAsync(string sessionName, FactoryServerConnectionInfo? connectionInfo)
    {
        var content = new DeleteSaveSessionContent(sessionName);
        return await ExecuteVoidRequestAsync(content, connectionInfo);
    }

    async Task<FactoryServerError?> IFactoryServerHttpService.LoadGameAsync(string saveName, bool enableAdvancedGameSettings, FactoryServerConnectionInfo? connectionInfo)
    {
        var content = new LoadGameContent(saveName, enableAdvancedGameSettings);
        return await ExecuteVoidRequestAsync(content, connectionInfo);
    }

    async Task<FactoryServerError?> IFactoryServerHttpService.UploadSaveGameAsync(string saveName, bool loadSaveGame, bool enableAdvancedGameSettings, Stream saveGameFile, FactoryServerConnectionInfo? connectionInfo)
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

        try
        {
            var error = JsonSerializer.Deserialize<FactoryServerError>(responseContent, FactoryServerContent.FactoryServerJsonOptions);
            return (null, error);
        }
        catch
        {
            var wrappedData = JsonSerializer.Deserialize<DataWrapper<TResult>>(responseContent, FactoryServerContent.FactoryServerJsonOptions)
                ?? throw new InvalidOperationException();

            return (wrappedData.Data, null);
        }
    }

    private static async Task<(TResult? Result, FactoryServerError? Error)> HandleStreamResponseAsync<TResult>(HttpResponseMessage response)
        where TResult : class
    {
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            return (null, null);

        var stream = await response.Content.ReadAsStreamAsync();
        if (response.Content.Headers.ContentType?.MediaType == MediaTypeNames.Application.Octet)
        {
            return (stream as TResult, null);
        }

        if (response.Content.Headers.ContentType?.MediaType == MediaTypeNames.Application.Json)
        {
            var error = await JsonSerializer.DeserializeAsync<FactoryServerError>(stream, FactoryServerContent.FactoryServerJsonOptions);
            return (null, error);
        }

        throw new InvalidOperationException();
    }

    private static async Task<FactoryServerError?> HandleVoidResponseAsync(HttpResponseMessage response)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            return null;

        try
        {
            var responseContent = await response.Content.ReadAsStreamAsync();
            var error = await JsonSerializer.DeserializeAsync<FactoryServerError>(responseContent, FactoryServerContent.FactoryServerJsonOptions);
            return error;
        }
        catch
        {
            return null;
        }
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