

namespace FactoryServerApi;

public interface IFactoryServerClient
{
    FactoryServerStateSnapshot CurrentServerState { get; }
    bool IsServerClaimed { get; }

    event EventHandler<FactoryServerErrorEventArgs>? ErrorOccurred;
    event EventHandler<FactoryServerStateChangedEventArgs>? ServerStateChanged;

    Task StartConnectionAsync(CancellationToken cancellationToken = default);
}
