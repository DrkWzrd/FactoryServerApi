
namespace FactoryServerApi.Udp;

public interface IFactoryServerUdpClient
{
    event EventHandler<FactoryServerStateResponse>? ServerStateReceived;
    event EventHandler<Exception>? ErrorOccurred;

    Task PollServerStateAsync(ulong? cookie = null, CancellationToken cancellationToken = default);
    Task PollServerStateAsync(TimeSpan duration, TimeSpan delayBetweenPolls, bool repeatPoll = false, int messagesByPoll = 1, Func<ulong>? cookieGenerator = null, CancellationToken cancellationToken = default);
    Task StartListeningAsync(CancellationToken cancellationToken = default);
    Task StopListeningAsync();
}
