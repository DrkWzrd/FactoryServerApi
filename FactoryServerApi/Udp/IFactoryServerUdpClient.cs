
namespace FactoryServerApi.Udp;

public interface IFactoryServerUdpClient
{
    event EventHandler<FactoryServerStateResponse>? ServerStateReceived;
    event EventHandler<Exception>? ErrorOccurred;

    Task SendPollingMessageAsync(ulong? cookie = null, CancellationToken cancellationToken = default);
    Task StartServerPollingAsync(TimeSpan duration, TimeSpan delayBetweenPolls, int messagesByPoll = 1, ValueTask<ulong>? cookieGenerator = null, CancellationToken cancellationToken = default);
    Task StartListeningAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
    Task StopListeningAsync();
    Task StartServerPollingAsync(TimeSpan delayBetweenPolls, int messagesByPoll = 2, ValueTask<ulong>? cookieGenerator = null, CancellationToken cancellationToken = default);
}
