
namespace FactoryServerApi.Udp;

public interface IFactoryServerUdpClient : IDisposable
{
    event EventHandler<FactoryServerStateUdpResponse>? ServerStateReceived;
    event EventHandler<Exception>? ErrorOccurred;

    Task SendPollingMessageAsync(ulong? cookie = null, CancellationToken cancellationToken = default);
    Task StartPollingAsync(TimeSpan duration = default, ICookieGenerator? cookieGenerator = null, CancellationToken cancellationToken = default);
    Task StopPollingAsync();
    Task ReceiveMessageAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
}
