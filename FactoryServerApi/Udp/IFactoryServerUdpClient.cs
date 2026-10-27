
namespace FactoryServerApi.Udp;

public interface IFactoryServerUdpClient : IDisposable
{
    event EventHandler<FactoryServerStateUdpResponse>? ServerStateReceived;
    event EventHandler<Exception>? ErrorOccurred;

    Task SendPollingMessageAsync(ulong? cookie = null, CancellationToken cancellationToken = default);
    Task StartPollingAsync(TimeSpan duration = default, ICookieGenerator? cookieGenerator = null, CancellationToken cancellationToken = default);
    Task StopPollingAsync();
    /// <summary>
    /// Waits for the next valid server state response and returns it.
    /// Throws <see cref="InvalidDataException"/> if an invalid message is received.
    /// </summary>
    Task<FactoryServerStateUdpResponse> ReceiveMessageAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
}
