using FactoryServerApi.Http.Requests.Contents;
using FactoryServerApi.Http.Responses;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;

namespace FactoryServerApi.Http;

internal class FactoryServerHttpClient : IFactoryServerHttpClient
{

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _settingsApiPath;
    private readonly SemaphoreSlim _httpClientSemaphore = new(1, 1);
    private readonly Uri _baseAddress;
    private AuthenticationData _authenticationData = AuthenticationData.Empty;
    private FactoryGamePlayerId? _playerId;

    public AuthenticationData AuthenticationData => _authenticationData;

    public FactoryGamePlayerId? PlayerId => _playerId;

    public bool IsServerClaimed { get; private set; }

    public FactoryServerHttpClient(IHttpClientFactory httpClientFactory, string host, int port, IOptions<HttpOptions> options)
    {
        _baseAddress = new Uri($"{host}:{port}");
        _settingsApiPath = options.Value.ApiPath;
        _httpClientFactory = httpClientFactory;
    }

    internal async Task CheckIfServerIsClaimed(CancellationToken cancellationToken)
    {
        var initialAdminLogin = await PasswordlessLoginAsync(FactoryServerPrivilegeLevel.InitialAdmin, cancellationToken);

        if (initialAdminLogin.Error is not null)
            IsServerClaimed = true;
        else
        {
            IsServerClaimed = false;
            await SetAuthenticationDataPrivateAsync(new AuthenticationData(initialAdminLogin.Data!), cancellationToken);
        }
    }

    public async Task<FactoryServerError?> SetAuthenticationDataAndVerifyAsync(AuthenticationData authenticationData, CancellationToken cancellationToken = default)
    {
        await SetAuthenticationDataPrivateAsync(authenticationData, cancellationToken);

        var verification = await VerifyAuthenticationTokenAsync(cancellationToken);
        if (verification is not null)
        {
            await SetAuthenticationDataPrivateAsync(AuthenticationData.Empty, cancellationToken);
            return verification;
        }
        return null;
    }

    private async Task SetAuthenticationDataPrivateAsync(AuthenticationData authData, CancellationToken cancellationToken = default)
    {
        await _httpClientSemaphore.WaitAsync(cancellationToken);
        try
        {
            _authenticationData = authData;
        }
        finally
        {
            _httpClientSemaphore.Release();
        }
    }

    public async Task SetPlayerIdAsync(FactoryGamePlayerId? playerId, CancellationToken cancellationToken = default)
    {
        await _httpClientSemaphore.WaitAsync(cancellationToken);
        try
        {
            _playerId = playerId;
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

    private async Task<FactoryServerResponseContent<LoginData>> PasswordlessLoginAsync(FactoryServerPrivilegeLevel minimumPrivilegeLevel, CancellationToken cancellationToken = default)
    {
        var content = new PasswordlessLoginRequestContent(minimumPrivilegeLevel);
        return await ExecuteRequestWithEnsuredJsonResponse<LoginData>(content, cancellationToken);
    }

    private async Task<FactoryServerResponseContent<LoginData>> PasswordLoginAsync(FactoryServerPrivilegeLevel minimumPrivilegeLevel, ReadOnlyMemory<char>? password, CancellationToken cancellationToken = default)
    {
        var content = new PasswordLoginRequestContent(minimumPrivilegeLevel, password);
        return await ExecuteRequestWithEnsuredJsonResponse<LoginData>(content, cancellationToken);
    }

    public async Task<FactoryServerError?> ClientLoginAsync(ReadOnlyMemory<char>? password, CancellationToken cancellationToken = default)
    {
        if (password is null)
        {
            var loginResult = await PasswordlessLoginAsync(FactoryServerPrivilegeLevel.Client, cancellationToken);
            if (loginResult.Error is not null)
                return loginResult.Error;

            await SetAuthenticationDataPrivateAsync(new AuthenticationData(loginResult.Data!), cancellationToken);
        }
        else
        {
            var loginResult = await PasswordLoginAsync(FactoryServerPrivilegeLevel.Client, password.Value, cancellationToken);
            if (loginResult.Error is not null)
                return loginResult.Error;

            await SetAuthenticationDataPrivateAsync(new AuthenticationData(loginResult.Data!), cancellationToken);
        }
        return null;
    }

    public async Task<FactoryServerError?> AdministratorLoginAsync(ReadOnlyMemory<char> password, CancellationToken cancellationToken = default)
    {
        var loginResult = await PasswordLoginAsync(FactoryServerPrivilegeLevel.Administrator, password, cancellationToken);
        if (loginResult.Error is not null)
            return loginResult.Error;

        await SetAuthenticationDataPrivateAsync(new AuthenticationData(loginResult.Data!), cancellationToken);
        return null;
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
        if (_authenticationData.TokenPrivilegeLevel == FactoryServerPrivilegeLevel.InitialAdmin)
        {
            var content = new ClaimServerRequestContent(serverName, adminPassword);
            return await ExecuteRequestWithEnsuredJsonResponse<LoginData>(content, cancellationToken);
        }

        var initialAdminData = await PasswordlessLoginAsync(FactoryServerPrivilegeLevel.InitialAdmin, cancellationToken);
        if (initialAdminData.Error is not null)
            return initialAdminData;

        await SetAuthenticationDataPrivateAsync(new AuthenticationData(initialAdminData.Data!), cancellationToken);

        return await ClaimServerAsync(serverName, adminPassword, cancellationToken);
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

    private async Task<FactoryServerResponseContent<TData>> ExecuteRequestWithEnsuredJsonResponse<TData>(HttpContent content, CancellationToken cancellationToken = default)
        where TData : FactoryServerResponseContentData
    {
        await _httpClientSemaphore.WaitAsync(cancellationToken);

        try
        {
            var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
            SetupHttpClient(httpClient);
            var response = await httpClient.PostAsync(_settingsApiPath, content, cancellationToken);
            return await HandleEnsuredJsonResponseAsync<TData>(response, cancellationToken);
        }
        finally
        {
            _httpClientSemaphore.Release();
        }
    }

    private async Task<FactoryServerResponseContent<DownloadSaveGameData>> ExecuteRequestWithMaybeOctetResponse(HttpContent content, CancellationToken cancellationToken = default)
    {
        await _httpClientSemaphore.WaitAsync(cancellationToken);

        try
        {
            var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
            SetupHttpClient(httpClient);
            var response = await httpClient.PostAsync(_settingsApiPath, content, cancellationToken);
            return await HandleMaybeOctetResponseAsync(response, cancellationToken);
        }
        finally
        {
            _httpClientSemaphore.Release();
        }
    }

    private async Task<FactoryServerError?> ExecuteMaybeVoidRequestAsync(HttpContent content, CancellationToken cancellationToken = default)
    {
        await _httpClientSemaphore.WaitAsync(cancellationToken);
        try
        {
            var httpClient = _httpClientFactory.CreateClient("factoryServerHttpClient");
            SetupHttpClient(httpClient);
            var response = await httpClient.PostAsync(_settingsApiPath, content, cancellationToken);
            return await HandleMaybeVoidResponseAsync(response, cancellationToken);
        }
        finally
        {
            _httpClientSemaphore.Release();
        }
    }

    private void SetupHttpClient(HttpClient hClient)
    {
        hClient.BaseAddress = _baseAddress;
        if (_authenticationData.AuthenticationToken is not null)
            hClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authenticationData.AuthenticationToken);

        if (_playerId is not null)
            hClient.DefaultRequestHeaders.Add("X-FactoryGame-PlayerId", _playerId.Value.ToString());
    }

    private static async Task<FactoryServerResponseContent<TData>> HandleEnsuredJsonResponseAsync<TData>(HttpResponseMessage response, CancellationToken cancellationToken = default)
        where TData : FactoryServerResponseContentData
    {
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            throw new InvalidOperationException();

        if (response.Content.Headers.ContentType?.MediaType != MediaTypeNames.Application.Json)
            throw new InvalidOperationException();

        var result = await response.Content.ReadFromJsonAsync<FactoryServerResponseContent<TData>>(cancellationToken)
            ?? throw new InvalidDataException();

        return result;
    }

    private static async Task<FactoryServerError?> HandleMaybeVoidResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            return null;

        if (response.Content.Headers.ContentType?.MediaType != MediaTypeNames.Application.Json)
            throw new InvalidOperationException();

        var result = await response.Content.ReadFromJsonAsync<FactoryServerError>(cancellationToken)
            ?? throw new InvalidDataException();

        return result;
    }

    private static async Task<FactoryServerResponseContent<DownloadSaveGameData>> HandleMaybeOctetResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            throw new InvalidOperationException();

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        if(response.Content.Headers.ContentType?.MediaType == MediaTypeNames.Application.Octet)
        {
            var data = new DownloadSaveGameData(stream);
            return new FactoryServerResponseContent<DownloadSaveGameData>()
            {
                Data = data,
            };
        }

        var result = await response.Content.ReadFromJsonAsync<FactoryServerResponseContent<DownloadSaveGameData>>(cancellationToken)
            ?? throw new InvalidDataException();

        return result;
    }
}