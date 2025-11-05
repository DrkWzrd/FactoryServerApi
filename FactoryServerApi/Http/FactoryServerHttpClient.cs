using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using FactoryServerApi.Http.Requests.Contents;
using FactoryServerApi.Http.Responses;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace FactoryServerApi.Http;

internal class FactoryServerHttpClient : IFactoryServerHttpClient
{
    private const string _playerIdHeader = "X-FactoryGame-PlayerId";

    private readonly HttpClient _httpClient;
    private readonly Lock _stateLock = new();
    private readonly HttpOptions _options;

    public string? AuthenticationToken { get; private set; }

    public FactoryGamePlayerId? PlayerId { get; private set; }

    public FactoryServerHttpClient(IHttpClientFactory httpClientFactory, string host, int port, IOptions<HttpOptions> options)
    {
        _options = options.Value;
        _httpClient = httpClientFactory.CreateClient("factoryServerHttpClient");
        _httpClient.BaseAddress = new UriBuilder("https", host, port).Uri;
    }

    public Task SetPlayerIdAsync(FactoryGamePlayerId playerId, CancellationToken cancellationToken = default)
    {
        lock (_stateLock)
        {
            PlayerId = playerId;
        }
        return Task.CompletedTask;
    }

    public Task ClearPlayerIdAsync(CancellationToken cancellationToken = default)
    {
        lock (_stateLock)
        {
            PlayerId = null;
        }
        return Task.CompletedTask;
    }

    public Task SetAuthenticationTokenAsync(string authenticationToken, CancellationToken cancellationToken = default)
    {
        lock (_stateLock)
        {
            AuthenticationToken = authenticationToken;
        }
        return Task.CompletedTask;
    }

    public Task ClearAuthenticationTokenAsync(CancellationToken cancellationToken = default)
    {
        lock (_stateLock)
        {
            AuthenticationToken = null;
        }
        return Task.CompletedTask;
    }

    public async Task<FactoryServerResponseContent<HealthCheckData>> HealthCheckAsync(string? clientCustomData, CancellationToken cancellationToken = default)
    {
        HealthCheckRequestContent content = new(clientCustomData);
        return await ExecuteRequestWithEnsuredJsonResponse<HealthCheckData>(content, cancellationToken);
    }

    public async Task<FactoryServerError?> VerifyAuthenticationTokenAsync(CancellationToken cancellationToken = default)
    {
        VerifyAuthenticationTokenRequestContent content = new();
        return await ExecuteMaybeVoidRequestAsync(content, cancellationToken);
    }

    public async Task<FactoryServerResponseContent<LoginData>> PasswordlessLoginAsync(FactoryServerPrivilegeLevel minimumPrivilegeLevel, CancellationToken cancellationToken = default)
    {
        PasswordlessLoginRequestContent content = new(minimumPrivilegeLevel);
        return await ExecuteRequestWithEnsuredJsonResponse<LoginData>(content, cancellationToken);
    }

    public async Task<FactoryServerResponseContent<LoginData>> PasswordLoginAsync(FactoryServerPrivilegeLevel minimumPrivilegeLevel, ReadOnlyMemory<char> password, CancellationToken cancellationToken = default)
    {
        PasswordLoginRequestContent content = new(minimumPrivilegeLevel, password);
        return await ExecuteRequestWithEnsuredJsonResponse<LoginData>(content, cancellationToken);
    }

    public async Task<FactoryServerResponseContent<QueryServerStateData>> QueryServerStateAsync(CancellationToken cancellationToken = default)
    {
        QueryServerStateRequestContent content = new();
        return await ExecuteRequestWithEnsuredJsonResponse<QueryServerStateData>(content, cancellationToken);
    }

    public async Task<FactoryServerResponseContent<GetServerOptionsData>> GetServerOptionsAsync(CancellationToken cancellationToken = default)
    {
        GetServerOptionsRequestContent content = new();
        return await ExecuteRequestWithEnsuredJsonResponse<GetServerOptionsData>(content, cancellationToken);
    }

    public async Task<FactoryServerResponseContent<GetAdvancedGameSettingsData>> GetAdvancedGameSettingsAsync(CancellationToken cancellationToken = default)
    {
        GetAdvancedGameSettingsRequestContent content = new();
        return await ExecuteRequestWithEnsuredJsonResponse<GetAdvancedGameSettingsData>(content, cancellationToken);
    }

    public async Task<FactoryServerResponseContent<LoginData>> ClaimServerAsync(string serverName, ReadOnlyMemory<char> adminPassword, CancellationToken cancellationToken = default)
    {
        ClaimServerRequestContent content = new(serverName, adminPassword);
        return await ExecuteRequestWithEnsuredJsonResponse<LoginData>(content, cancellationToken);
    }

    public async Task<FactoryServerResponseContent<RunCommandData>> RunCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        RunCommandRequestContent content = new(command);
        return await ExecuteRequestWithEnsuredJsonResponse<RunCommandData>(content, cancellationToken);
    }

    public async Task<FactoryServerResponseContent<EnumerateSessionsData>> EnumerateSessionsAsync(CancellationToken cancellationToken = default)
    {
        EnumerateSessionsRequestContent content = new();
        return await ExecuteRequestWithEnsuredJsonResponse<EnumerateSessionsData>(content, cancellationToken);
    }

    public async Task<FactoryServerResponseContent<DownloadSaveGameData>> DownloadSaveGameAsync(string saveName, CancellationToken cancellationToken = default)
    {
        DownloadSaveGameRequestContent content = new(saveName);
        return await ExecuteRequestWithMaybeOctetResponse(content, cancellationToken);
    }

    public async Task<FactoryServerError?> ApplyAdvancedGameSettingsAsync(Dictionary<string, string> appliedAdvancedGameSettings, CancellationToken cancellationToken = default)
    {
        ApplyAdvancedGameSettingsRequestContent content = new(appliedAdvancedGameSettings);
        return await ExecuteMaybeVoidRequestAsync(content, cancellationToken);
    }

    public async Task<FactoryServerError?> RenameServerAsync(string serverName, CancellationToken cancellationToken = default)
    {
        RenameServerRequestContent content = new(serverName);
        return await ExecuteMaybeVoidRequestAsync(content, cancellationToken);
    }

    public async Task<FactoryServerError?> SetClientPasswordAsync(ReadOnlyMemory<char>? password, CancellationToken cancellationToken = default)
    {
        SetClientPasswordRequestContent content = new(password);
        return await ExecuteMaybeVoidRequestAsync(content, cancellationToken);
    }

    public async Task<FactoryServerError?> SetAdminPasswordAsync(ReadOnlyMemory<char> password, string authenticationToken, CancellationToken cancellationToken = default)
    {
        SetAdminPasswordRequestContent content = new(password, authenticationToken);
        return await ExecuteMaybeVoidRequestAsync(content, cancellationToken);
    }

    public async Task<FactoryServerError?> SetAutoLoadSessionNameAsync(string sessionName, CancellationToken cancellationToken = default)
    {
        SetAutoLoadSessionNameRequestContent content = new(sessionName);
        return await ExecuteMaybeVoidRequestAsync(content, cancellationToken);
    }

    public async Task<FactoryServerError?> ShutdownAsync(CancellationToken cancellationToken = default)
    {
        ShutdownRequestContent content = new();
        return await ExecuteMaybeVoidRequestAsync(content, cancellationToken);
    }

    public async Task<FactoryServerError?> ApplyServerOptions(Dictionary<string, string> updatedServerOptions, CancellationToken cancellationToken = default)
    {
        ApplyServerOptionsRequestContent content = new(updatedServerOptions);
        return await ExecuteMaybeVoidRequestAsync(content, cancellationToken);
    }

    public async Task<FactoryServerError?> CreateNewGameAsync(ServerNewGameData newGameData, CancellationToken cancellationToken = default)
    {
        CreateNewGameRequestContent content = new(newGameData);
        return await ExecuteMaybeVoidRequestAsync(content, cancellationToken);
    }

    public async Task<FactoryServerError?> SaveGameAsync(string saveName, CancellationToken cancellationToken = default)
    {
        SaveGameRequestContent content = new(saveName);
        return await ExecuteMaybeVoidRequestAsync(content, cancellationToken);
    }

    public async Task<FactoryServerError?> DeleteSaveFileAsync(string saveName, CancellationToken cancellationToken = default)
    {
        DeleteSaveFileRequestContent content = new(saveName);
        return await ExecuteMaybeVoidRequestAsync(content, cancellationToken);
    }

    public async Task<FactoryServerError?> DeleteSaveSessionAsync(string sessionName, CancellationToken cancellationToken = default)
    {
        DeleteSaveSessionRequestContent content = new(sessionName);
        return await ExecuteMaybeVoidRequestAsync(content, cancellationToken);
    }

    public async Task<FactoryServerError?> LoadGameAsync(string saveName, bool enableAdvancedGameSettings, CancellationToken cancellationToken = default)
    {
        LoadGameRequestContent content = new(saveName, enableAdvancedGameSettings);
        return await ExecuteMaybeVoidRequestAsync(content, cancellationToken);
    }

    public async Task<FactoryServerError?> UploadSaveGameAsync(string saveName, bool loadSaveGame, bool enableAdvancedGameSettings,
        Stream saveGameFile, CancellationToken cancellationToken = default)
    {
        UploadSaveGameRequestContent content = new(saveName, loadSaveGame, enableAdvancedGameSettings, saveGameFile);
        return await ExecuteMaybeVoidRequestAsync(content, cancellationToken);
    }

    private async Task<FactoryServerResponseContent<TData>> ExecuteRequestWithEnsuredJsonResponse<TData>(HttpContent content, CancellationToken cancellationToken = default)
        where TData : FactoryServerResponseContentData
    {
        var response = await ExecuteRequestAsync(content, cancellationToken);
        return await HandleEnsuredJsonResponseAsync<TData>(response, cancellationToken);
    }

    private async Task<FactoryServerResponseContent<DownloadSaveGameData>> ExecuteRequestWithMaybeOctetResponse(HttpContent content, CancellationToken cancellationToken = default)
    {
        var response = await ExecuteRequestAsync(content, cancellationToken);
        return await HandleMaybeOctetResponseAsync(response, cancellationToken);
    }

    private async Task<FactoryServerError?> ExecuteMaybeVoidRequestAsync(HttpContent content, CancellationToken cancellationToken = default)
    {
        var response = await ExecuteRequestAsync(content, cancellationToken);
        return await HandleMaybeVoidResponseAsync(response, cancellationToken);
    }

    private async Task<HttpResponseMessage> ExecuteRequestAsync(HttpContent content, CancellationToken cancellationToken)
    {
        string? authToken;
        FactoryGamePlayerId? playerId;
        lock (_stateLock)
        {
            authToken = AuthenticationToken;
            playerId = PlayerId;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.ApiPath)
        {
            Content = content,
        };

        request.Headers.Authorization = authToken is not null
            ? new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, authToken)
            : null;

        if (playerId is not null)
            request.Headers.Add(_playerIdHeader, playerId.Value.ToString());

        return await _httpClient.SendAsync(request, cancellationToken);
    }

    private static async Task<FactoryServerResponseContent<TData>> HandleEnsuredJsonResponseAsync<TData>(HttpResponseMessage response, CancellationToken cancellationToken = default)
        where TData : FactoryServerResponseContentData
    {
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            throw new InvalidOperationException("No content found in response when content was expected.");

        if (response.Content.Headers.ContentType?.MediaType?.StartsWith(MediaTypeNames.Application.Json, StringComparison.OrdinalIgnoreCase) != true)
        {
            throw new InvalidOperationException(
                $"Unexpected response content media type: '{response.Content.Headers.ContentType}'. " +
                $"Expected 'application/json'. Status: {response.StatusCode}.");
        }

        FactoryServerResponseContent<TData> result = await response.Content.ReadFromJsonAsync<FactoryServerResponseContent<TData>>(cancellationToken)
            ?? throw new InvalidDataException("Unexpected response data from server.");

        return result;
    }

    private static async Task<FactoryServerError?> HandleMaybeVoidResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            return null;

        if (response.Content.Headers.ContentType?.MediaType?.StartsWith(MediaTypeNames.Application.Json, StringComparison.OrdinalIgnoreCase) != true)
        {
            throw new InvalidOperationException(
                $"Unexpected response content media type: '{response.Content.Headers.ContentType}'. " +
                $"Expected 'application/json' for error. Status: {response.StatusCode}.");
        }

        FactoryServerError result = await response.Content.ReadFromJsonAsync<FactoryServerError>(cancellationToken)
            ?? throw new InvalidDataException("Unexpected response data from server.");

        return result;
    }

    private static async Task<FactoryServerResponseContent<DownloadSaveGameData>> HandleMaybeOctetResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            throw new InvalidOperationException("No content found in response when content was expected.");

        Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        if (response.Content.Headers.ContentType?.MediaType == MediaTypeNames.Application.Octet)
        {
            DownloadSaveGameData data = new(stream);
            return new FactoryServerResponseContent<DownloadSaveGameData>()
            {
                Data = data,
            };
        }

        FactoryServerResponseContent<DownloadSaveGameData> result = await response.Content.ReadFromJsonAsync<FactoryServerResponseContent<DownloadSaveGameData>>(cancellationToken)
            ?? throw new InvalidDataException("Unexpected response data from server.");

        return result;
    }

}