using System.IO;
using FactoryServerApiNew.Http;
using FactoryServerApiNew.Http.Responses;
using FactoryServerApiNew.Udp;

namespace FactoryServerApiNew;

/// <summary>
/// High-level wrapper around <see cref="IFactoryServerClient"/> that owns the
/// authentication lifecycle. After <see cref="InitializeAsync"/> the session is
/// guaranteed to be authenticated with Administrator privileges: it claims an
/// unclaimed server (prompting the user for the server name and admin password)
/// and logs in as admin (re-using the in-memory password first, then prompting
/// the user). Because the underlying client transparently refreshes expired
/// tokens, every delegated operation runs with a valid token.
/// </summary>
public sealed class FactoryServerSession : IFactoryServerClient
{
    private readonly IFactoryServerClient _client;
    private readonly FactoryServerCredentialPrompt _prompt;

    public FactoryServerSession(IFactoryServerClient client, FactoryServerCredentialPrompt prompt)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _prompt = prompt ?? throw new ArgumentNullException(nameof(prompt));
    }

    // --- Login flow ---

    /// <inheritdoc/>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // 1. Check the claim state and start UDP polling.
        await _client.InitializeAsync(cancellationToken);

        // 2. Claim an unclaimed server, prompting the user for name + admin password.
        if (_client.MustClaim)
        {
            (string serverName, ReadOnlyMemory<char> adminPassword) = await _prompt.ClaimServer();
            if (!await _client.ClaimServerAsync(serverName, adminPassword, cancellationToken))
                throw new InvalidOperationException("Failed to claim the server.");
        }

        // 3. Ensure Administrator privileges. The underlying client re-logs in with
        //    the in-memory password first; only if that fails do we prompt the user.
        if (_client.CurrentPrivilegeLevel is not (FactoryServerPrivilegeLevel.Administrator or FactoryServerPrivilegeLevel.APIToken))
        {
            if (!await _client.EnsureAdminAuthenticationAsync(cancellationToken))
            {
                ReadOnlyMemory<char> adminPassword = await _prompt.AdminPassword();
                if (!await _client.AdministratorLoginAsync(adminPassword, cancellationToken))
                    throw new InvalidOperationException("Failed to log in as Administrator.");
            }
        }
    }

    // --- Delegated surface ---

    public event EventHandler<FactoryServerStateUdpResponse>? ServerStateReceived
    {
        add => _client.ServerStateReceived += value;
        remove => _client.ServerStateReceived -= value;
    }

    public event EventHandler<Exception>? ErrorOccurred
    {
        add => _client.ErrorOccurred += value;
        remove => _client.ErrorOccurred -= value;
    }

    public FactoryServerPrivilegeLevel CurrentPrivilegeLevel => _client.CurrentPrivilegeLevel;
    public FactoryServerStateUdpResponse? CurrentServerState => _client.CurrentServerState;
    public FactoryGamePlayerId? PlayerId => _client.PlayerId;
    public bool IsClaimed => _client.IsClaimed;
    public bool MustClaim => _client.MustClaim;

    // --- Polling ---

    public Task StartPollingAsync(CancellationToken cancellationToken = default) => _client.StartPollingAsync(cancellationToken);
    public Task StopPollingAsync() => _client.StopPollingAsync();

    // --- Authentication ---

    public Task<bool> AdministratorLoginAsync(ReadOnlyMemory<char> password, CancellationToken cancellationToken = default)
        => _client.AdministratorLoginAsync(password, cancellationToken);

    public Task<bool> ClientLoginAsync(ReadOnlyMemory<char>? password, CancellationToken cancellationToken = default)
        => _client.ClientLoginAsync(password, cancellationToken);

    public Task<bool> ClaimServerAsync(string serverName, ReadOnlyMemory<char> adminPassword, CancellationToken cancellationToken = default)
        => _client.ClaimServerAsync(serverName, adminPassword, cancellationToken);

    public Task<bool> SetAdminPasswordAsync(ReadOnlyMemory<char> password, string newAuthenticationToken, CancellationToken cancellationToken = default)
        => _client.SetAdminPasswordAsync(password, newAuthenticationToken, cancellationToken);

    public Task SetAuthenticationTokenAsync(string authToken, CancellationToken cancellationToken = default)
        => _client.SetAuthenticationTokenAsync(authToken, cancellationToken);

    public Task ClearLoginDataAsync(CancellationToken cancellationToken = default)
        => _client.ClearLoginDataAsync(cancellationToken);

    public Task<bool> GetIsAuthenticationTokenValidAsync(CancellationToken cancellationToken = default)
        => _client.GetIsAuthenticationTokenValidAsync(cancellationToken);

    public Task<bool> EnsureAdminAuthenticationAsync(CancellationToken cancellationToken = default)
        => _client.EnsureAdminAuthenticationAsync(cancellationToken);

    public Task SetPlayerIdAsync(FactoryGamePlayerId playerId, CancellationToken cancellationToken = default)
        => _client.SetPlayerIdAsync(playerId, cancellationToken);

    public Task ClearPlayerIdAsync(CancellationToken cancellationToken = default)
        => _client.ClearPlayerIdAsync(cancellationToken);

    // --- Server state ---

    public Task<bool> GetIsServerOnlineAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        => _client.GetIsServerOnlineAsync(timeout, cancellationToken);

    public Task<FactoryServerStateUdpResponse?> GetServerStateAsync(CancellationToken cancellationToken = default)
        => _client.GetServerStateAsync(cancellationToken);

    public Task<FactoryServerResponseContent<HealthCheckData>> HealthCheckAsync(string? clientCustomData, CancellationToken cancellationToken = default)
        => _client.HealthCheckAsync(clientCustomData, cancellationToken);

    public Task<FactoryServerResponseContent<QueryServerStateData>> QueryServerStateAsync(CancellationToken cancellationToken = default)
        => _client.QueryServerStateAsync(cancellationToken);

    public Task<FactoryServerResponseContent<GetServerOptionsData>> GetServerOptionsAsync(CancellationToken cancellationToken = default)
        => _client.GetServerOptionsAsync(cancellationToken);

    public Task<FactoryServerResponseContent<GetAdvancedGameSettingsData>> GetAdvancedGameSettingsAsync(CancellationToken cancellationToken = default)
        => _client.GetAdvancedGameSettingsAsync(cancellationToken);

    // --- Admin functions (guarded) ---

    public Task<FactoryServerError?> ApplyAdvancedGameSettingsAsync(Dictionary<string, string> appliedAdvancedGameSettings, CancellationToken cancellationToken = default)
        => _client.ApplyAdvancedGameSettingsAsync(appliedAdvancedGameSettings, cancellationToken);

    public Task<FactoryServerError?> RenameServerAsync(string serverName, CancellationToken cancellationToken = default)
        => _client.RenameServerAsync(serverName, cancellationToken);

    public Task<FactoryServerError?> SetClientPasswordAsync(ReadOnlyMemory<char>? password, CancellationToken cancellationToken = default)
        => _client.SetClientPasswordAsync(password, cancellationToken);

    public Task<FactoryServerError?> SetAutoLoadSessionNameAsync(string sessionName, CancellationToken cancellationToken = default)
        => _client.SetAutoLoadSessionNameAsync(sessionName, cancellationToken);

    public Task<FactoryServerResponseContent<RunCommandData>> RunCommandAsync(string command, CancellationToken cancellationToken = default)
        => _client.RunCommandAsync(command, cancellationToken);

    public Task<FactoryServerError?> ShutdownAsync(CancellationToken cancellationToken = default)
        => _client.ShutdownAsync(cancellationToken);

    public Task<FactoryServerError?> ApplyServerOptionsAsync(Dictionary<string, string> updatedServerOptions, CancellationToken cancellationToken = default)
        => _client.ApplyServerOptionsAsync(updatedServerOptions, cancellationToken);

    public Task<FactoryServerError?> CreateNewGameAsync(ServerNewGameData newGameData, CancellationToken cancellationToken = default)
        => _client.CreateNewGameAsync(newGameData, cancellationToken);

    public Task<FactoryServerError?> SaveGameAsync(string saveName, CancellationToken cancellationToken = default)
        => _client.SaveGameAsync(saveName, cancellationToken);

    public Task<FactoryServerError?> DeleteSaveFileAsync(string saveName, CancellationToken cancellationToken = default)
        => _client.DeleteSaveFileAsync(saveName, cancellationToken);

    public Task<FactoryServerError?> DeleteSaveSessionAsync(string sessionName, CancellationToken cancellationToken = default)
        => _client.DeleteSaveSessionAsync(sessionName, cancellationToken);

    public Task<FactoryServerResponseContent<EnumerateSessionsData>> EnumerateSessionsAsync(CancellationToken cancellationToken = default)
        => _client.EnumerateSessionsAsync(cancellationToken);

    public Task<FactoryServerError?> LoadGameAsync(string saveName, bool enableAdvancedGameSettings, CancellationToken cancellationToken = default)
        => _client.LoadGameAsync(saveName, enableAdvancedGameSettings, cancellationToken);

    public Task<FactoryServerError?> UploadSaveGameAsync(string saveName, bool loadSaveGame, bool enableAdvancedGameSettings, Stream saveGameFile, CancellationToken cancellationToken = default)
        => _client.UploadSaveGameAsync(saveName, loadSaveGame, enableAdvancedGameSettings, saveGameFile, cancellationToken);

    public Task<FactoryServerResponseContent<DownloadSaveGameData>> DownloadSaveGameAsync(string saveName, CancellationToken cancellationToken = default)
        => _client.DownloadSaveGameAsync(saveName, cancellationToken);

    // --- Disposal ---

    public void Dispose() => _client.Dispose();
}
