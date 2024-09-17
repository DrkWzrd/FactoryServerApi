using FactoryServerApi.Http;
using FactoryServerApi.Udp;

namespace FactoryServerApi;

public interface IFactoryServerManager
{
    FactoryServerManagerOptions Options { get; }

    IFactoryServerApi CurrentServerApi { get; }

    event EventHandler<Exception>? ErrorOccurred;
    event EventHandler<FactoryServerStateResponse>? ServerStateReceived;

    Task ConnectToServerAsync(CancellationToken cancellationToken = default);
    Task<FactoryServerInfo?> GetCurrentServerInfoAsync();
}
