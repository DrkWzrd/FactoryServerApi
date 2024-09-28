

using FactoryServerApi.Http;

namespace FactoryServerApi;

public interface IFactoryServerClient
{
    FactoryGamePlayerId? PlayerId { get; }
    FactoryServerPrivilegeLevel ClientCurrentPrivilegeLevel { get; }
    IFactoryServerHttpClient ApiAccessPoint { get; }

    event EventHandler<FactoryServerErrorEventArgs>? ErrorOccurred;
    event EventHandler<FactoryServerStateChangedEventArgs>? ServerStateChanged;

    Task AdministratorLoginAsync(ReadOnlyMemory<char> password, CancellationToken cancellationToken = default);
    Task ClaimServerAsync(string serverName, ReadOnlyMemory<char> adminPassword, CancellationToken cancellationToken = default);
    Task ClearLoginDataAsync(CancellationToken cancellationToken = default);
    Task ClearPlayerIdAsync(CancellationToken cancellationToken = default);
    Task ClientLoginAsync(ReadOnlyMemory<char>? password, CancellationToken cancellationToken = default);
    Task<FactoryServerInfoSnapshot?> GetCurrentServerStateAsync(CancellationToken cancellationToken = default);
    Task<bool> GetIsAuthenticationTokenValidAsync(CancellationToken cancellationToken = default);
    Task<bool> GetIsServerOnlineAsync(TimeSpan timeout, bool checkUdp = true, CancellationToken cancellationToken = default);
    Task SetAuthenticationTokenAsync(string authToken, CancellationToken cancellationToken = default);
    Task SetPlayerIdAsync(FactoryGamePlayerId playerId, CancellationToken cancellationToken = default);
    Task InitializeClientAsync(CancellationToken cancellationToken = default);
}
