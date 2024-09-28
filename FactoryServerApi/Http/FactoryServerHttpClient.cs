using FactoryServerApi.Http.Requests.Contents;
using FactoryServerApi.Http.Responses;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;

namespace FactoryServerApi.Http;

internal class FactoryServerHttpClient : IFactoryServerHttpClient
{
    private const string _playerIdHeader = "X-FactoryGame-PlayerId";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HttpOptions _options;
    private readonly SemaphoreSlim _httpClientSemaphore = new(1, 1);
    private readonly Uri _baseAddress;

    public string? AuthenticationToken { get; private set; }

    public FactoryGamePlayerId? PlayerId { get; private set; }

    public FactoryServerHttpClient(IHttpClientFactory httpClientFactory, string host, int port, IOptions<HttpOptions> options)
    {
        _baseAddress = new UriBuilder("https", host, port).Uri;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task SetPlayerIdAsync(FactoryGamePlayerId playerId, CancellationToken cancellationToken = default)
    {
        await _httpClientSemaphore.WaitAsync(cancellationToken);
        try
        {
            PlayerId = playerId;
        }
        finally
        {
            _httpClientSemaphore.Release();
        }
    }

    public async Task ClearPlayerIdAsync(CancellationToken cancellationToken = default)
    {
        await _httpClientSemaphore.WaitAsync(cancellationToken);
        try
        {
            PlayerId = null;
        }
        finally
        {
            _httpClientSemaphore.Release();
        }
    }

    public async Task SetAuthenticationTokenAsync(string authenticationToken, CancellationToken cancellationToken = default)
    {
        await _httpClientSemaphore.WaitAsync(cancellationToken);
        try
        {
            AuthenticationToken = authenticationToken;
        }
        finally
        {
            _httpClientSemaphore.Release();
        }
    }

    public async Task ClearAuthenticationTokenAsync(CancellationToken cancellationToken = default)
    {
        await _httpClientSemaphore.WaitAsync(cancellationToken);
        try
        {
            AuthenticationToken = null;
        }
        finally
        {
            _httpClientSemaphore.Release();
        }
    }

    public async Task<FactoryServerResponseContent<HealthCheckData>> HealthCheckAsync(string? clientCustomData, CancellationToken cancellationToken = default)
    {
        var content = new HealthCheckRequestContent(clientCustomData);
        return await ExecuteRequestWithEnsuredJsonResponse<HealthCheckData>(content, cancellationToken);
    }

    public async Task<FactoryServerError?> VerifyAuthenticationTokenAsync(CancellationToken cancellationToken = default)
    {
        var content = new VerifyAuthenticationTokenRequestContent();
        return await ExecuteMaybeVoidRequestAsync(content, cancellationToken);
    }

    public async Task<FactoryServerResponseContent<LoginData>> PasswordlessLoginAsync(FactoryServerPrivilegeLevel minimumPrivilegeLevel, CancellationToken cancellationToken = default)
    {
        var content = new PasswordlessLoginRequestContent(minimumPrivilegeLevel);
        return await ExecuteRequestWithEnsuredJsonResponse<LoginData>(content, cancellationToken);
    }

    public async Task<FactoryServerResponseContent<LoginData>> PasswordLoginAsync(FactoryServerPrivilegeLevel minimumPrivilegeLevel, ReadOnlyMemory<char> password, CancellationToken cancellationToken = default)
    {
        var content = new PasswordLoginRequestContent(minimumPrivilegeLevel, password);
        return await ExecuteRequestWithEnsuredJsonResponse<LoginData>(content, cancellationToken);
    }

    public async Task<FactoryServerResponseContent<QueryServerStateData>> QueryServerStateAsync(CancellationToken cancellationToken = default)
    {
        var content = new QueryServerStateRequestContent();
        return await ExecuteRequestWithEnsuredJsonResponse<QueryServerStateData>(content, cancellationToken);
    }

    public async Task<FactoryServerResponseContent<GetServerOptionsData>> GetServerOptionsAsync(CancellationToken cancellationToken = default)
    {
        var content = new GetServerOptionsRequestContent();
        return await ExecuteRequestWithEnsuredJsonResponse<GetServerOptionsData>(content, cancellationToken);
    }

    public async Task<FactoryServerResponseContent<GetAdvancedGameSettingsData>> GetAdvancedGameSettingsAsync(CancellationToken cancellationToken = default)
    {
        var content = new GetAdvancedGameSettingsRequestContent();
        return await ExecuteRequestWithEnsuredJsonResponse<GetAdvancedGameSettingsData>(content, cancellationToken);
    }

    public async Task<FactoryServerResponseContent<LoginData>> ClaimServerAsync(string serverName, ReadOnlyMemory<char> adminPassword, CancellationToken cancellationToken = default)
    {
        var content = new ClaimServerRequestContent(serverName, adminPassword);
        return await ExecuteRequestWithEnsuredJsonResponse<LoginData>(content, cancellationToken);
    }

    public async Task<FactoryServerResponseContent<RunCommandData>> RunCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        var content = new RunCommandRequestContent(command);
        return await ExecuteRequestWithEnsuredJsonResponse<RunCommandData>(content, cancellationToken);
    }

    public async Task<FactoryServerResponseContent<EnumerateSessionsData>> EnumerateSessionsAsync(CancellationToken cancellationToken = default)
    {
        var content = new EnumerateSessionsRequestContent();
        return await ExecuteRequestWithEnsuredJsonResponse<EnumerateSessionsData>(content, cancellationToken);
    }

    public async Task<FactoryServerResponseContent<DownloadSaveGameData>> DownloadSaveGameAsync(string saveName, CancellationToken cancellationToken = default)
    {
        var content = new DownloadSaveGameRequestContent(saveName);
        return await ExecuteRequestWithMaybeOctetResponse(content, cancellationToken);
    }

    public async Task<FactoryServerError?> ApplyAdvancedGameSettingsAsync(Dictionary<string, string> appliedAdvancedGameSettings, CancellationToken cancellationToken = default)
    {
        var content = new ApplyAdvancedGameSettingsRequestContent(appliedAdvancedGameSettings);
        return await ExecuteMaybeVoidRequestAsync(content, cancellationToken);
    }

    public async Task<FactoryServerError?> RenameServerAsync(string serverName, CancellationToken cancellationToken = default)
    {
        var content = new RenameServerRequestContent(serverName);
        return await ExecuteMaybeVoidRequestAsync(content, cancellationToken);
    }

    public async Task<FactoryServerError?> SetClientPasswordAsync(ReadOnlyMemory<char>? password, CancellationToken cancellationToken = default)
    {
        var content = new SetClientPasswordRequestContent(password);
        return await ExecuteMaybeVoidRequestAsync(content, cancellationToken);
    }

    public async Task<FactoryServerError?> SetAdminPasswordAsync(ReadOnlyMemory<char> password, string authenticationToken, CancellationToken cancellationToken = default)
    {
        var content = new SetAdminPasswordRequestContent(password, authenticationToken);
        return await ExecuteMaybeVoidRequestAsync(content, cancellationToken);
    }

    public async Task<FactoryServerError?> SetAutoLoadSessionNameAsync(string sessionName, CancellationToken cancellationToken = default)
    {
        var content = new SetAutoLoadSessionNameRequestContent(sessionName);
        return await ExecuteMaybeVoidRequestAsync(content, cancellationToken);
    }

    public async Task<FactoryServerError?> ShutdownAsync(CancellationToken cancellationToken = default)
    {
        var content = new ShutdownRequestContent();
        return await ExecuteMaybeVoidRequestAsync(content, cancellationToken);
    }

    public async Task<FactoryServerError?> ApplyServerOptions(Dictionary<string, string> updatedServerOptions, CancellationToken cancellationToken = default)
    {
        var content = new ApplyServerOptionsRequestContent(updatedServerOptions);
        return await ExecuteMaybeVoidRequestAsync(content, cancellationToken);
    }

    public async Task<FactoryServerError?> CreateNewGameAsync(ServerNewGameData newGameData, CancellationToken cancellationToken = default)
    {
        var content = new CreateNewGameRequestContent(newGameData);
        return await ExecuteMaybeVoidRequestAsync(content, cancellationToken);
    }

    public async Task<FactoryServerError?> SaveGameAsync(string saveName, CancellationToken cancellationToken = default)
    {
        var content = new SaveGameRequestContent(saveName);
        return await ExecuteMaybeVoidRequestAsync(content, cancellationToken);
    }

    public async Task<FactoryServerError?> DeleteSaveFileAsync(string saveName, CancellationToken cancellationToken = default)
    {
        var content = new DeleteSaveFileRequestContent(saveName);
        return await ExecuteMaybeVoidRequestAsync(content, cancellationToken);
    }

    public async Task<FactoryServerError?> DeleteSaveSessionAsync(string sessionName, CancellationToken cancellationToken = default)
    {
        var content = new DeleteSaveSessionRequestContent(sessionName);
        return await ExecuteMaybeVoidRequestAsync(content, cancellationToken);
    }

    public async Task<FactoryServerError?> LoadGameAsync(string saveName, bool enableAdvancedGameSettings, CancellationToken cancellationToken = default)
    {
        var content = new LoadGameRequestContent(saveName, enableAdvancedGameSettings);
        return await ExecuteMaybeVoidRequestAsync(content, cancellationToken);
    }

    public async Task<FactoryServerError?> UploadSaveGameAsync(string saveName, bool loadSaveGame, bool enableAdvancedGameSettings,
        Stream saveGameFile, CancellationToken cancellationToken = default)
    {
        var content = new UploadSaveGameRequestContent(saveName, loadSaveGame, enableAdvancedGameSettings, saveGameFile);
        return await ExecuteMaybeVoidRequestAsync(content, cancellationToken);
    }

    private void SetupHttpClient(HttpClient hClient)
    {
        hClient.BaseAddress = _baseAddress;
        if (AuthenticationToken is not null)
            hClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, AuthenticationToken);

        if (PlayerId is not null)
            hClient.DefaultRequestHeaders.Add(_playerIdHeader, PlayerId.Value.ToString());
    }

    private async Task<FactoryServerResponseContent<TData>> ExecuteRequestWithEnsuredJsonResponse<TData>(HttpContent content, CancellationToken cancellationToken = default)
        where TData : FactoryServerResponseContentData
    {
        var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
        await _httpClientSemaphore.WaitAsync(cancellationToken);

        try
        {
            SetupHttpClient(httpClient);
            var response = await httpClient.PostAsync(_options.ApiPath, content, cancellationToken);
            return await HandleEnsuredJsonResponseAsync<TData>(response, cancellationToken);
        }
        finally
        {
            _httpClientSemaphore.Release();
        }
    }

    private async Task<FactoryServerResponseContent<DownloadSaveGameData>> ExecuteRequestWithMaybeOctetResponse(HttpContent content, CancellationToken cancellationToken = default)
    {
        var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
        await _httpClientSemaphore.WaitAsync(cancellationToken);

        try
        {
            SetupHttpClient(httpClient);
            var response = await httpClient.PostAsync(_options.ApiPath, content, cancellationToken);
            return await HandleMaybeOctetResponseAsync(response, cancellationToken);
        }
        finally
        {
            _httpClientSemaphore.Release();
        }
    }

    private async Task<FactoryServerError?> ExecuteMaybeVoidRequestAsync(HttpContent content, CancellationToken cancellationToken = default)
    {
        var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
        await _httpClientSemaphore.WaitAsync(cancellationToken);
        try
        {
            SetupHttpClient(httpClient);
            var response = await httpClient.PostAsync(_options.ApiPath, content, cancellationToken);
            return await HandleMaybeVoidResponseAsync(response, cancellationToken);
        }
        finally
        {
            _httpClientSemaphore.Release();
        }
    }

    private static async Task<FactoryServerResponseContent<TData>> HandleEnsuredJsonResponseAsync<TData>(HttpResponseMessage response, CancellationToken cancellationToken = default)
        where TData : FactoryServerResponseContentData
    {
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            throw new InvalidOperationException("No content found in response when content was expected.");

        if (response.Content.Headers.ContentType?.MediaType != MediaTypeNames.Application.Json)
            throw new InvalidOperationException("Unexpected response content media type.");

        var result = await response.Content.ReadFromJsonAsync<FactoryServerResponseContent<TData>>(cancellationToken)
            ?? throw new InvalidDataException("Unexpected response data from server.");

        return result;
    }

    private static async Task<FactoryServerError?> HandleMaybeVoidResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            return null;

        if (response.Content.Headers.ContentType?.MediaType != MediaTypeNames.Application.Json)
            throw new InvalidOperationException("Unexpected response content media type.");

        var result = await response.Content.ReadFromJsonAsync<FactoryServerError>(cancellationToken)
            ?? throw new InvalidDataException("Unexpected response data from server.");

        return result;
    }

    private static async Task<FactoryServerResponseContent<DownloadSaveGameData>> HandleMaybeOctetResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            throw new InvalidOperationException("No content found in response when content was expected.");

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        if (response.Content.Headers.ContentType?.MediaType == MediaTypeNames.Application.Octet)
        {
            var data = new DownloadSaveGameData(stream);
            return new FactoryServerResponseContent<DownloadSaveGameData>()
            {
                Data = data,
            };
        }

        var result = await response.Content.ReadFromJsonAsync<FactoryServerResponseContent<DownloadSaveGameData>>(cancellationToken)
            ?? throw new InvalidDataException("Unexpected response data from server.");

        return result;
    }

}