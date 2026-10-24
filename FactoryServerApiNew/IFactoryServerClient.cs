using FactoryServerApiNew.Http;
using FactoryServerApiNew.Http.Responses;
using FactoryServerApiNew.Udp;
using System.Net.Sockets;

namespace FactoryServerApiNew;

/// <summary>
/// Merged access point to the Dedicated Server API: the HTTPS API for detailed state and control,
/// and the UDP Lightweight Query for polling the server state with minimal overhead.
/// Both endpoints listen on the same server port (7777 by default).
/// Admin functions are guarded by the login flow: if the current token lacks Administrator
/// privileges, the client attempts to re-login with the last admin password used.
/// </summary>
public interface IFactoryServerClient : IDisposable
{
    /// <summary>
    /// Raised for every server state received while UDP polling is active.
    /// </summary>
    event EventHandler<FactoryServerStateUdpResponse>? ServerStateReceived;

    /// <summary>
    /// Raised when the UDP endpoint encounters an error, e.g. no server response
    /// after a number of attempts.
    /// </summary>
    event EventHandler<Exception>? ErrorOccurred;

    /// <summary>
    /// The privilege level granted by the current authentication token.
    /// </summary>
    FactoryServerPrivilegeLevel CurrentPrivilegeLevel { get; }

    /// <summary>
    /// The last server state received via UDP, if any.
    /// </summary>
    FactoryServerStateUdpResponse? CurrentServerState { get; }

    // --- Polling ---

    /// <summary>
    /// Starts continuous UDP state polling. Raises <see cref="ServerStateReceived"/>
    /// for every response and <see cref="ErrorOccurred"/> on failure.
    /// </summary>
    Task StartPollingAsync(CancellationToken cancellationToken = default);

    Task StopPollingAsync();

    /// <summary>
    /// The player ID currently set for join/encryption ticket requests, if any.
    /// </summary>
    FactoryGamePlayerId? PlayerId { get; }

    /// <summary>
    /// Whether the server has an Admin Password set, i.e. it is claimed.
    /// Claiming is irreversible by the client side, so this state is persisted
    /// per server in the local application data folder and restored on startup.
    /// </summary>
    bool IsClaimed { get; }

    /// <summary>
    /// Initializes the client: checks whether the server is claimed by attempting a
    /// passwordless InitialAdmin login, stores the InitialAdmin token (if the server
    /// is not claimed) for the later claim, and starts UDP state polling.
    /// Throws <see cref="InvalidOperationException"/> if the claim check fails for
    /// a reason other than the server already being claimed.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    // --- Authentication ---

    /// <summary>
    /// Logs in with the server's Admin Password.
    /// </summary>
    Task<bool> AdministratorLoginAsync(ReadOnlyMemory<char> password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs in as a player. A null <paramref name="password"/> attempts a passwordless login,
    /// which is only possible if the Client Protection Password is not set.
    /// </summary>
    Task<bool> ClientLoginAsync(ReadOnlyMemory<char>? password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Claims an unclaimed server. An InitialAdmin token is first acquired via passwordless login
    /// (only possible while the server is not claimed); after a successful claim the server issues
    /// a new token that replaces it, dropping the InitialAdmin privilege.
    /// Returns false if the server is already claimed.
    /// </summary>
    Task<bool> ClaimServerAsync(string serverName, ReadOnlyMemory<char> adminPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the Admin Password. The server invalidates the token used for this request,
    /// so <paramref name="newAuthenticationToken"/> is set as the current token on success.
    /// </summary>
    Task<bool> SetAdminPasswordAsync(ReadOnlyMemory<char> password, string newAuthenticationToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the authentication token, e.g. a long-lived API token issued by the
    /// <c>server.GenerateAPIToken</c> console command for third party applications.
    /// </summary>
    Task SetAuthenticationTokenAsync(string authToken, CancellationToken cancellationToken = default);

    Task ClearLoginDataAsync(CancellationToken cancellationToken = default);

    Task<bool> GetIsAuthenticationTokenValidAsync(CancellationToken cancellationToken = default);

    Task SetPlayerIdAsync(FactoryGamePlayerId playerId, CancellationToken cancellationToken = default);

    Task ClearPlayerIdAsync(CancellationToken cancellationToken = default);

    // --- Server state ---

    /// <summary>
    /// Checks if the server is reachable: a UDP poll first, then an HTTPS health check as fallback.
    /// </summary>
    Task<bool> GetIsServerOnlineAsync(TimeSpan timeout, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current server state with a single UDP poll. Returns null if no response arrives.
    /// </summary>
    Task<FactoryServerStateUdpResponse?> GetServerStateAsync(CancellationToken cancellationToken = default);

    Task<FactoryServerResponseContent<HealthCheckData>> HealthCheckAsync(string? clientCustomData, CancellationToken cancellationToken = default);

    Task<FactoryServerResponseContent<QueryServerStateData>> QueryServerStateAsync(CancellationToken cancellationToken = default);

    Task<FactoryServerResponseContent<GetServerOptionsData>> GetServerOptionsAsync(CancellationToken cancellationToken = default);

    Task<FactoryServerResponseContent<GetAdvancedGameSettingsData>> GetAdvancedGameSettingsAsync(CancellationToken cancellationToken = default);

    // --- Admin functions (guarded) ---

    Task<FactoryServerError?> ApplyAdvancedGameSettingsAsync(Dictionary<string, string> appliedAdvancedGameSettings, CancellationToken cancellationToken = default);

    Task<FactoryServerError?> RenameServerAsync(string serverName, CancellationToken cancellationToken = default);

    Task<FactoryServerError?> SetClientPasswordAsync(ReadOnlyMemory<char>? password, CancellationToken cancellationToken = default);

    Task<FactoryServerError?> SetAutoLoadSessionNameAsync(string sessionName, CancellationToken cancellationToken = default);

    Task<FactoryServerResponseContent<RunCommandData>> RunCommandAsync(string command, CancellationToken cancellationToken = default);

    Task<FactoryServerError?> ShutdownAsync(CancellationToken cancellationToken = default);

    Task<FactoryServerError?> ApplyServerOptionsAsync(Dictionary<string, string> updatedServerOptions, CancellationToken cancellationToken = default);

    Task<FactoryServerError?> CreateNewGameAsync(ServerNewGameData newGameData, CancellationToken cancellationToken = default);

    Task<FactoryServerError?> SaveGameAsync(string saveName, CancellationToken cancellationToken = default);

    Task<FactoryServerError?> DeleteSaveFileAsync(string saveName, CancellationToken cancellationToken = default);

    Task<FactoryServerError?> DeleteSaveSessionAsync(string sessionName, CancellationToken cancellationToken = default);

    Task<FactoryServerResponseContent<EnumerateSessionsData>> EnumerateSessionsAsync(CancellationToken cancellationToken = default);

    Task<FactoryServerError?> LoadGameAsync(string saveName, bool enableAdvancedGameSettings, CancellationToken cancellationToken = default);

    Task<FactoryServerError?> UploadSaveGameAsync(string saveName, bool loadSaveGame, bool enableAdvancedGameSettings, Stream saveGameFile, CancellationToken cancellationToken = default);

    Task<FactoryServerResponseContent<DownloadSaveGameData>> DownloadSaveGameAsync(string saveName, CancellationToken cancellationToken = default);
}

internal class FactoryServerClient : IFactoryServerClient
{
    private static readonly TimeSpan DefaultUdpTimeout = TimeSpan.FromSeconds(5);

    private readonly IFactoryServerHttpClient _http;
    private readonly IFactoryServerUdpClient _udp;
    private readonly FactoryServerClaimStore _claimStore;
    private ReadOnlyMemory<char>? _adminPassword;
    private string? _initialAdminToken;
    private volatile bool _isClaimed;
    private volatile FactoryServerStateUdpResponse? _currentServerState;

    public FactoryServerPrivilegeLevel CurrentPrivilegeLevel => AuthenticationTokenHelper.GetTokenLevel(_http.AuthenticationToken?.AsMemory());

    public FactoryGamePlayerId? PlayerId => _http.PlayerId;

    public bool IsClaimed => _isClaimed;

    public FactoryServerStateUdpResponse? CurrentServerState => _currentServerState;

    public event EventHandler<FactoryServerStateUdpResponse>? ServerStateReceived;
    public event EventHandler<Exception>? ErrorOccurred;

    internal FactoryServerClient(IFactoryServerHttpClient httpClient, IFactoryServerUdpClient udpClient, string host, int port)
    {
        _http = httpClient;
        _udp = udpClient;
        _claimStore = new FactoryServerClaimStore(host, port);
        _isClaimed = _claimStore.IsClaimed;
        _initialAdminToken = _claimStore.InitialAdminToken;
        _adminPassword = _claimStore.AdminPassword?.AsMemory();
        _udp.ServerStateReceived += Udp_ServerStateReceived;
        _udp.ErrorOccurred += Udp_ErrorOccurred;
    }

    private void PersistClaimState()
    {
        _claimStore.IsClaimed = _isClaimed;
        _claimStore.InitialAdminToken = _initialAdminToken;
        _claimStore.AdminPassword = _adminPassword?.ToString();
        _claimStore.Save();
    }

    // --- Authentication ---

    public async Task<bool> AdministratorLoginAsync(ReadOnlyMemory<char> password, CancellationToken cancellationToken = default)
    {
        FactoryServerResponseContent<LoginData> result = await _http.PasswordLoginAsync(FactoryServerPrivilegeLevel.Administrator, password, cancellationToken);
        if (result.Data is null)
            return false;

        _adminPassword = password;
        PersistClaimState();
        await _http.SetAuthenticationTokenAsync(result.Data.AuthenticationToken, cancellationToken);
        return true;
    }

    public async Task<bool> ClientLoginAsync(ReadOnlyMemory<char>? password, CancellationToken cancellationToken = default)
    {
        Task<FactoryServerResponseContent<LoginData>> loginTask = password is null
            ? _http.PasswordlessLoginAsync(FactoryServerPrivilegeLevel.Client, cancellationToken)
            : _http.PasswordLoginAsync(FactoryServerPrivilegeLevel.Client, password.Value, cancellationToken);

        FactoryServerResponseContent<LoginData> result = await loginTask;
        if (result.Data is null)
            return false;

        await _http.SetAuthenticationTokenAsync(result.Data.AuthenticationToken, cancellationToken);
        return true;
    }

    public async Task<bool> ClaimServerAsync(string serverName, ReadOnlyMemory<char> adminPassword, CancellationToken cancellationToken = default)
    {
        // InitialAdmin can only be acquired via passwordless login while the server has no
        // Admin Password set, i.e. it is not claimed yet. Prefer the token stored by
        // InitializeAsync; any other error is a real failure.
        string? initialAdminToken = _initialAdminToken;
        if (initialAdminToken is null)
        {
            FactoryServerResponseContent<LoginData> initialAdminResult = await _http.PasswordlessLoginAsync(FactoryServerPrivilegeLevel.InitialAdmin, cancellationToken);
            if (initialAdminResult.Data is null)
                return false; // already claimed (passwordless_login_not_possible) or a transport error
            initialAdminToken = initialAdminResult.Data.AuthenticationToken;
        }

        await _http.SetAuthenticationTokenAsync(initialAdminToken, cancellationToken);

        FactoryServerResponseContent<LoginData> claimResult = await _http.ClaimServerAsync(serverName, adminPassword, cancellationToken);
        if (claimResult.Data is null)
            return false; // the claim failed; the InitialAdmin token is kept for a retry

        // The server issues a new token: the InitialAdmin privilege must be dropped in favor of it.
        _initialAdminToken = null;
        _isClaimed = true;
        _adminPassword = adminPassword;
        PersistClaimState();
        await _http.SetAuthenticationTokenAsync(claimResult.Data.AuthenticationToken, cancellationToken);
        return true;
    }

    public async Task<bool> SetAdminPasswordAsync(ReadOnlyMemory<char> password, string newAuthenticationToken, CancellationToken cancellationToken = default)
    {
        FactoryServerError? error = await _http.SetAdminPasswordAsync(password, newAuthenticationToken, cancellationToken);
        if (error is not null)
            return false;

        // The token used for this request was invalidated by the server.
        _adminPassword = password;
        PersistClaimState();
        await _http.SetAuthenticationTokenAsync(newAuthenticationToken, cancellationToken);
        return true;
    }

    public Task SetAuthenticationTokenAsync(string authToken, CancellationToken cancellationToken = default)
        => _http.SetAuthenticationTokenAsync(authToken, cancellationToken);

    public Task ClearLoginDataAsync(CancellationToken cancellationToken = default)
        => _http.ClearAuthenticationTokenAsync(cancellationToken);

    public async Task<bool> GetIsAuthenticationTokenValidAsync(CancellationToken cancellationToken = default)
    {
        if (_http.AuthenticationToken is null)
            return false;

        return await _http.VerifyAuthenticationTokenAsync(cancellationToken: cancellationToken) is null;
    }

    public Task SetPlayerIdAsync(FactoryGamePlayerId playerId, CancellationToken cancellationToken = default)
        => _http.SetPlayerIdAsync(playerId, cancellationToken);

    public Task ClearPlayerIdAsync(CancellationToken cancellationToken = default)
        => _http.ClearPlayerIdAsync(cancellationToken);

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Claiming is irreversible by the client side: if this client already claimed
        // the server, the persisted state says so and no check is needed.
        if (!_isClaimed)
        {
            // A passwordless InitialAdmin login is only possible while the server is
            // not claimed. The token is persisted until the admin claims the server.
            FactoryServerResponseContent<LoginData> initialAdminResult = await _http.PasswordlessLoginAsync(FactoryServerPrivilegeLevel.InitialAdmin, cancellationToken);
            if (initialAdminResult.Error is not null)
            {
                if (initialAdminResult.Error.ErrorCode == "passwordless_login_not_possible")
                {
                    _isClaimed = true;
                }
                else
                {
                    throw new InvalidOperationException($"Failed to check if the server is claimed. Error: {initialAdminResult.Error.ErrorCode} - {initialAdminResult.Error.ErrorMessage}");
                }
            }
            else
            {
                _initialAdminToken = initialAdminResult.Data!.AuthenticationToken;
            }

            PersistClaimState();
        }

        await _udp.StartPollingAsync(cancellationToken: cancellationToken);
    }

    public Task StartPollingAsync(CancellationToken cancellationToken = default)
        => _udp.StartPollingAsync(cancellationToken: cancellationToken);

    public Task StopPollingAsync()
        => _udp.StopPollingAsync();

    private void Udp_ServerStateReceived(object? sender, FactoryServerStateUdpResponse response)
    {
        _currentServerState = response;
        ServerStateReceived?.Invoke(this, response);
    }

    private void Udp_ErrorOccurred(object? sender, Exception ex) => ErrorOccurred?.Invoke(this, ex);

    // --- Server state ---

    public async Task<bool> GetIsServerOnlineAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (await TryReceiveUdpStateAsync(timeout, cancellationToken) is not null)
            return true;

        // UDP is unreachable (or the server is starting up): fall back to the HTTPS health check.
        using CancellationTokenSource timeoutCts = new(timeout);
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        try
        {
            FactoryServerResponseContent<HealthCheckData> health = await _http.HealthCheckAsync(null, linked.Token);
            return health.Error is null;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    public Task<FactoryServerStateUdpResponse?> GetServerStateAsync(CancellationToken cancellationToken = default)
        => TryReceiveUdpStateAsync(DefaultUdpTimeout, cancellationToken);

    public Task<FactoryServerResponseContent<HealthCheckData>> HealthCheckAsync(string? clientCustomData, CancellationToken cancellationToken = default)
        => _http.HealthCheckAsync(clientCustomData, cancellationToken);

    public Task<FactoryServerResponseContent<QueryServerStateData>> QueryServerStateAsync(CancellationToken cancellationToken = default)
        => _http.QueryServerStateAsync(cancellationToken);

    public Task<FactoryServerResponseContent<GetServerOptionsData>> GetServerOptionsAsync(CancellationToken cancellationToken = default)
        => _http.GetServerOptionsAsync(cancellationToken);

    public Task<FactoryServerResponseContent<GetAdvancedGameSettingsData>> GetAdvancedGameSettingsAsync(CancellationToken cancellationToken = default)
        => _http.GetAdvancedGameSettingsAsync(cancellationToken);

    // --- Admin functions (guarded) ---

    public async Task<FactoryServerError?> ApplyAdvancedGameSettingsAsync(Dictionary<string, string> appliedAdvancedGameSettings, CancellationToken cancellationToken = default)
    {
        FactoryServerError? authError = await EnsureAdminAsync(cancellationToken);
        if (authError is not null)
            return authError;

        return await _http.ApplyAdvancedGameSettingsAsync(appliedAdvancedGameSettings, cancellationToken);
    }

    public async Task<FactoryServerError?> RenameServerAsync(string serverName, CancellationToken cancellationToken = default)
    {
        FactoryServerError? authError = await EnsureAdminAsync(cancellationToken);
        if (authError is not null)
            return authError;

        return await _http.RenameServerAsync(serverName, cancellationToken);
    }

    public async Task<FactoryServerError?> SetClientPasswordAsync(ReadOnlyMemory<char>? password, CancellationToken cancellationToken = default)
    {
        FactoryServerError? authError = await EnsureAdminAsync(cancellationToken);
        if (authError is not null)
            return authError;

        return await _http.SetClientPasswordAsync(password, cancellationToken);
    }

    public async Task<FactoryServerError?> SetAutoLoadSessionNameAsync(string sessionName, CancellationToken cancellationToken = default)
    {
        FactoryServerError? authError = await EnsureAdminAsync(cancellationToken);
        if (authError is not null)
            return authError;

        return await _http.SetAutoLoadSessionNameAsync(sessionName, cancellationToken);
    }

    public async Task<FactoryServerResponseContent<RunCommandData>> RunCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        FactoryServerError? authError = await EnsureAdminAsync(cancellationToken);
        if (authError is not null)
            return new FactoryServerResponseContent<RunCommandData>(null, authError);

        return await _http.RunCommandAsync(command, cancellationToken);
    }

    public async Task<FactoryServerError?> ShutdownAsync(CancellationToken cancellationToken = default)
    {
        FactoryServerError? authError = await EnsureAdminAsync(cancellationToken);
        if (authError is not null)
            return authError;

        return await _http.ShutdownAsync(cancellationToken);
    }

    public async Task<FactoryServerError?> ApplyServerOptionsAsync(Dictionary<string, string> updatedServerOptions, CancellationToken cancellationToken = default)
    {
        FactoryServerError? authError = await EnsureAdminAsync(cancellationToken);
        if (authError is not null)
            return authError;

        return await _http.ApplyServerOptions(updatedServerOptions, cancellationToken);
    }

    public async Task<FactoryServerError?> CreateNewGameAsync(ServerNewGameData newGameData, CancellationToken cancellationToken = default)
    {
        FactoryServerError? authError = await EnsureAdminAsync(cancellationToken);
        if (authError is not null)
            return authError;

        return await _http.CreateNewGameAsync(newGameData, cancellationToken);
    }

    public async Task<FactoryServerError?> SaveGameAsync(string saveName, CancellationToken cancellationToken = default)
    {
        FactoryServerError? authError = await EnsureAdminAsync(cancellationToken);
        if (authError is not null)
            return authError;

        return await _http.SaveGameAsync(saveName, cancellationToken);
    }

    public async Task<FactoryServerError?> DeleteSaveFileAsync(string saveName, CancellationToken cancellationToken = default)
    {
        FactoryServerError? authError = await EnsureAdminAsync(cancellationToken);
        if (authError is not null)
            return authError;

        return await _http.DeleteSaveFileAsync(saveName, cancellationToken);
    }

    public async Task<FactoryServerError?> DeleteSaveSessionAsync(string sessionName, CancellationToken cancellationToken = default)
    {
        FactoryServerError? authError = await EnsureAdminAsync(cancellationToken);
        if (authError is not null)
            return authError;

        return await _http.DeleteSaveSessionAsync(sessionName, cancellationToken);
    }

    public async Task<FactoryServerResponseContent<EnumerateSessionsData>> EnumerateSessionsAsync(CancellationToken cancellationToken = default)
    {
        FactoryServerError? authError = await EnsureAdminAsync(cancellationToken);
        if (authError is not null)
            return new FactoryServerResponseContent<EnumerateSessionsData>(null, authError);

        return await _http.EnumerateSessionsAsync(cancellationToken);
    }

    public async Task<FactoryServerError?> LoadGameAsync(string saveName, bool enableAdvancedGameSettings, CancellationToken cancellationToken = default)
    {
        FactoryServerError? authError = await EnsureAdminAsync(cancellationToken);
        if (authError is not null)
            return authError;

        return await _http.LoadGameAsync(saveName, enableAdvancedGameSettings, cancellationToken);
    }

    public async Task<FactoryServerError?> UploadSaveGameAsync(string saveName, bool loadSaveGame, bool enableAdvancedGameSettings, Stream saveGameFile, CancellationToken cancellationToken = default)
    {
        FactoryServerError? authError = await EnsureAdminAsync(cancellationToken);
        if (authError is not null)
            return authError;

        return await _http.UploadSaveGameAsync(saveName, loadSaveGame, enableAdvancedGameSettings, saveGameFile, cancellationToken);
    }

    public async Task<FactoryServerResponseContent<DownloadSaveGameData>> DownloadSaveGameAsync(string saveName, CancellationToken cancellationToken = default)
    {
        FactoryServerError? authError = await EnsureAdminAsync(cancellationToken);
        if (authError is not null)
            return new FactoryServerResponseContent<DownloadSaveGameData>(null, authError);

        return await _http.DownloadSaveGameAsync(saveName, cancellationToken);
    }

    // --- Helpers ---

    /// <summary>
    /// Ensures the current token has Administrator (or API token) privileges.
    /// If not, attempts to re-login with the last admin password used.
    /// Returns null on success, a synthetic error otherwise.
    /// </summary>
    private async Task<FactoryServerError?> EnsureAdminAsync(CancellationToken cancellationToken)
    {
        if (CurrentPrivilegeLevel is FactoryServerPrivilegeLevel.Administrator or FactoryServerPrivilegeLevel.APIToken)
            return null;

        if (_adminPassword is not null && await AdministratorLoginAsync(_adminPassword.Value, cancellationToken))
            return null;

        return new FactoryServerError
        {
            ErrorCode = "not_authenticated",
            ErrorMessage = "Administrator authentication is required."
        };
    }

    private async Task<FactoryServerStateUdpResponse?> TryReceiveUdpStateAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        try
        {
            Task<FactoryServerStateUdpResponse> receiveTask = _udp.ReceiveMessageAsync(timeout, cancellationToken);
            await _udp.SendPollingMessageAsync(cancellationToken: cancellationToken);
            return await receiveTask;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (SocketException)
        {
            return null;
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    public void Dispose()
    {
        _udp.ServerStateReceived -= Udp_ServerStateReceived;
        _udp.ErrorOccurred -= Udp_ErrorOccurred;
        _udp.Dispose();
    }
}
