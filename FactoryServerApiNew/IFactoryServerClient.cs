using FactoryServerApiNew.Http;
using FactoryServerApiNew.Http.Responses;
using FactoryServerApiNew.Udp;
using System.Net.Sockets;

namespace FactoryServerApiNew;

/// <summary>
/// Merged access point to the Dedicated Server API: the HTTPS API for detailed state and control,
/// and the UDP Lightweight Query for polling the server state with minimal overhead.
/// Both endpoints listen on the same server port (7777 by default).
/// </summary>
public interface IFactoryServerClient : IDisposable
{
    /// <summary>
    /// The HTTPS API access point.
    /// </summary>
    IFactoryServerHttpClient Http { get; }

    /// <summary>
    /// The UDP Lightweight Query access point.
    /// </summary>
    IFactoryServerUdpClient Udp { get; }

    /// <summary>
    /// The privilege level granted by the current authentication token.
    /// </summary>
    FactoryServerPrivilegeLevel CurrentPrivilegeLevel { get; }

    /// <summary>
    /// The player ID currently set for join/encryption ticket requests, if any.
    /// </summary>
    FactoryGamePlayerId? PlayerId { get; }

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

    /// <summary>
    /// Checks if the server is reachable: a UDP poll first, then an HTTPS health check as fallback.
    /// </summary>
    Task<bool> GetIsServerOnlineAsync(TimeSpan timeout, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current server state with a single UDP poll. Returns null if no response arrives.
    /// </summary>
    Task<FactoryServerStateUdpResponse?> GetServerStateAsync(CancellationToken cancellationToken = default);
}

internal class FactoryServerClient : IFactoryServerClient
{
    private static readonly TimeSpan DefaultUdpTimeout = TimeSpan.FromSeconds(5);

    public IFactoryServerHttpClient Http { get; }
    public IFactoryServerUdpClient Udp { get; }

    public FactoryServerPrivilegeLevel CurrentPrivilegeLevel => AuthenticationTokenHelper.GetTokenLevel(Http.AuthenticationToken?.AsMemory());

    public FactoryGamePlayerId? PlayerId => Http.PlayerId;

    internal FactoryServerClient(IFactoryServerHttpClient httpClient, IFactoryServerUdpClient udpClient)
    {
        Http = httpClient;
        Udp = udpClient;
    }

    public async Task<bool> AdministratorLoginAsync(ReadOnlyMemory<char> password, CancellationToken cancellationToken = default)
    {
        FactoryServerResponseContent<LoginData> result = await Http.PasswordLoginAsync(FactoryServerPrivilegeLevel.Administrator, password, cancellationToken);
        if (result.Data is null)
            return false;

        await SetAuthenticationTokenAsync(result.Data.AuthenticationToken, cancellationToken);
        return true;
    }

    public async Task<bool> ClientLoginAsync(ReadOnlyMemory<char>? password, CancellationToken cancellationToken = default)
    {
        Task<FactoryServerResponseContent<LoginData>> loginTask = password is null
            ? Http.PasswordlessLoginAsync(FactoryServerPrivilegeLevel.Client, cancellationToken)
            : Http.PasswordLoginAsync(FactoryServerPrivilegeLevel.Client, password.Value, cancellationToken);

        FactoryServerResponseContent<LoginData> result = await loginTask;
        if (result.Data is null)
            return false;

        await SetAuthenticationTokenAsync(result.Data.AuthenticationToken, cancellationToken);
        return true;
    }

    public async Task<bool> ClaimServerAsync(string serverName, ReadOnlyMemory<char> adminPassword, CancellationToken cancellationToken = default)
    {
        // InitialAdmin can only be acquired via passwordless login while the server has no
        // Admin Password set, i.e. it is not claimed yet. Any other error is a real failure.
        FactoryServerResponseContent<LoginData> initialAdminResult = await Http.PasswordlessLoginAsync(FactoryServerPrivilegeLevel.InitialAdmin, cancellationToken);
        if (initialAdminResult.Data is null)
            return false; // already claimed (passwordless_login_not_possible) or a transport error

        await SetAuthenticationTokenAsync(initialAdminResult.Data.AuthenticationToken, cancellationToken);

        FactoryServerResponseContent<LoginData> claimResult = await Http.ClaimServerAsync(serverName, adminPassword, cancellationToken);
        if (claimResult.Data is null)
            return false; // the claim failed; the InitialAdmin token is still valid

        // The server issues a new token: the InitialAdmin privilege must be dropped in favor of it.
        await SetAuthenticationTokenAsync(claimResult.Data.AuthenticationToken, cancellationToken);
        return true;
    }

    public async Task<bool> SetAdminPasswordAsync(ReadOnlyMemory<char> password, string newAuthenticationToken, CancellationToken cancellationToken = default)
    {
        FactoryServerError? error = await Http.SetAdminPasswordAsync(password, newAuthenticationToken, cancellationToken);
        if (error is not null)
            return false;

        // The token used for this request was invalidated by the server.
        await SetAuthenticationTokenAsync(newAuthenticationToken, cancellationToken);
        return true;
    }

    public Task SetAuthenticationTokenAsync(string authToken, CancellationToken cancellationToken = default)
        => Http.SetAuthenticationTokenAsync(authToken, cancellationToken);

    public Task ClearLoginDataAsync(CancellationToken cancellationToken = default)
        => Http.ClearAuthenticationTokenAsync(cancellationToken);

    public Task SetPlayerIdAsync(FactoryGamePlayerId playerId, CancellationToken cancellationToken = default)
        => Http.SetPlayerIdAsync(playerId, cancellationToken);

    public Task ClearPlayerIdAsync(CancellationToken cancellationToken = default)
        => Http.ClearPlayerIdAsync(cancellationToken);

    public async Task<bool> GetIsAuthenticationTokenValidAsync(CancellationToken cancellationToken = default)
    {
        if (Http.AuthenticationToken is null)
            return false;

        return await Http.VerifyAuthenticationTokenAsync(cancellationToken: cancellationToken) is null;
    }

    public async Task<bool> GetIsServerOnlineAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (await TryReceiveUdpStateAsync(timeout, cancellationToken) is not null)
            return true;

        // UDP is unreachable (or the server is starting up): fall back to the HTTPS health check.
        using CancellationTokenSource timeoutCts = new(timeout);
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        try
        {
            FactoryServerResponseContent<HealthCheckData> health = await Http.HealthCheckAsync(null, linked.Token);
            return health.Error is null;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    public Task<FactoryServerStateUdpResponse?> GetServerStateAsync(CancellationToken cancellationToken = default)
        => TryReceiveUdpStateAsync(DefaultUdpTimeout, cancellationToken);

    private async Task<FactoryServerStateUdpResponse?> TryReceiveUdpStateAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        try
        {
            Task<FactoryServerStateUdpResponse> receiveTask = Udp.ReceiveMessageAsync(timeout, cancellationToken);
            await Udp.SendPollingMessageAsync(cancellationToken: cancellationToken);
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

    public void Dispose() => Udp.Dispose();
}
