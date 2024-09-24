

namespace FactoryServerApi;

public interface IFactoryServerClient
{
    FactoryGamePlayerId? PlayerId { get; }
    FactoryServerPrivilegeLevel ClientCurrentPrivilegeLevel { get; }

    event EventHandler<FactoryServerErrorEventArgs>? ErrorOccurred;
    event EventHandler<FactoryServerStateChangedEventArgs>? ServerStateChanged;

    Task AdministratorLoginAsync(ReadOnlyMemory<char> password, CancellationToken cancellationToken = default);
    Task ClaimServerAsync(string serverName, ReadOnlyMemory<char> adminPassword, CancellationToken cancellationToken = default);
    Task ClearLoginDataAsync(CancellationToken cancellationToken = default);
    Task ClearPlayerIdAsync(CancellationToken cancellationToken = default);
    Task ClientLoginAsync(ReadOnlyMemory<char>? password, CancellationToken cancellationToken = default);
    Task<FactoryServerInfoSnapshot?> GetCurrentServerStateAsync(CancellationToken cancellationToken = default);
    Task<bool> GetIsAuthenticationTokenValidAsync(CancellationToken cancellationToken = default);
    Task<bool> GetIsServerOnline(TimeSpan timeout, CancellationToken cancellationToken = default);
    Task SetAuthenticationTokenAsync(string authToken, CancellationToken cancellationToken = default);
    Task SetPlayerIdAsync(FactoryGamePlayerId playerId, CancellationToken cancellationToken = default);
    Task InitializeClientAsync(CancellationToken cancellationToken = default);
}
