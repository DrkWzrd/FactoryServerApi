
namespace FactoryServerApi.Udp;

public interface IFactoryServerUdpClient
{
    event EventHandler<FactoryServerStateUdpResponse>? ServerStateReceived;
    event EventHandler<Exception>? ErrorOccurred;

    Task StartPollingAsync(TimeSpan duration = default, ValueTask<ulong>? cookieGenerator = null, CancellationToken cancellationToken = default);
    Task StopPollingAsync();
}
