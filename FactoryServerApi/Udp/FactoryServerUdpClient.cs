using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace FactoryServerApi.Udp;

internal class FactoryServerUdpClient : IFactoryServerUdpClient
{
    private const ushort ProtocolMagic = 0xF6D5;
    private const byte ProtocolVersion = 1;
    private const byte TerminatorByte = 0x1;

    private readonly TimeProvider _tProvider;
    private readonly UdpClient _client;
    private readonly IPEndPoint _serverEndPoint;

    private CancellationTokenSource _listenerStopTokenSource;

    private readonly HashSet<ulong> _sentCookies;

    public int RetriesBeforeStopPolling { get; set; } = 3;

    public bool IsRunning { get; set; }

    private bool IsPolling { get; set; }

    public event EventHandler<FactoryServerStateResponse>? ServerStateReceived;
    public event EventHandler<Exception>? ErrorOccurred;

    public FactoryServerUdpClient(IPEndPoint serverEndPoint, TimeProvider timeProvider, int localListenPort = 7777)
    {
        _client = new UdpClient(new IPEndPoint(IPAddress.Any, localListenPort));
        _serverEndPoint = serverEndPoint;
        _tProvider = timeProvider;
        _sentCookies = [];
        _listenerStopTokenSource = new CancellationTokenSource();
    }

    public Task StartListeningAsync(TimeSpan receiveTimeout, CancellationToken cancellationToken = default)
    {
        if (IsRunning)
            return Task.CompletedTask;

        if (_listenerStopTokenSource.IsCancellationRequested || !_listenerStopTokenSource.TryReset())
            _listenerStopTokenSource = new CancellationTokenSource();

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _listenerStopTokenSource.Token);
        return StartListeningPrivateAsync(receiveTimeout, RetriesBeforeStopPolling, cts.Token);
    }

    public Task StopListeningAsync()
    {
        if (!IsRunning)
            return Task.CompletedTask;
        _sentCookies.Clear();
        return _listenerStopTokenSource.CancelAsync();
    }

    private async Task StartListeningPrivateAsync(TimeSpan timeout, int retriesAmount = 3, CancellationToken cancellationToken = default)
    {
        IsRunning = true;
        if (!IsPolling)
        {
            await Task.Delay(300, cancellationToken);
            _ = StartServerPollingPrivateAsync(timeout, 2, null, cancellationToken);
        }
        var actualTry = 1;
        while (!cancellationToken.IsCancellationRequested)
        {
            var cts = new CancellationTokenSource(timeout);
            var linkedCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
            try
            {
                UdpReceiveResult result = await _client.ReceiveAsync(linkedCTS.Token);

                Memory<byte> receivedData = result.Buffer;

                if (receivedData.Length < 22 || receivedData.Span[^1] != TerminatorByte)
                    throw new InvalidDataException("Invalid response format");

                if (cancellationToken.IsCancellationRequested)
                    break;

                //filter bad responses
                if (BinaryPrimitives.ReadUInt16LittleEndian(receivedData.Span[..2]) != ProtocolMagic
                    || receivedData.Span[2] != (byte)FactoryServerUdpMessageType.ServerStateResponse
                    || receivedData.Span[3] != ProtocolVersion
                    || receivedData.Span[^1] != TerminatorByte)
                    continue;

                var cookie = BinaryPrimitives.ReadUInt64LittleEndian(receivedData.Span.Slice(4, 8));

                //filter polls we didn't do
                if (!_sentCookies.Remove(cookie))
                    continue;

                var serverStateResponse = FactoryServerStateResponse.Parse(receivedData.Span[4..^1], _tProvider.GetUtcNow());
                OnServerStateReceived(serverStateResponse);
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == linkedCTS.Token)
            {
                if (actualTry > retriesAmount)
                    break;
                actualTry++;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
            }
        }
        IsRunning = false;
    }

    public Task StartServerPollingAsync(TimeSpan delayBetweenPolls, int messagesByPoll = 2, ValueTask<ulong>? cookieGenerator = null, CancellationToken cancellationToken = default)
    {
        if (IsPolling)
            return Task.CompletedTask;
        return StartServerPollingPrivateAsync(delayBetweenPolls, messagesByPoll, cookieGenerator, cancellationToken);
    }

    public async Task StartServerPollingAsync(TimeSpan duration, TimeSpan delayBetweenPolls, int messagesByPoll = 2, ValueTask<ulong>? cookieGenerator = null, CancellationToken cancellationToken = default)
    {
        if (IsPolling)
            return;
        var pollingCTS = new CancellationTokenSource(duration);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, pollingCTS.Token);
        await StartServerPollingPrivateAsync(delayBetweenPolls, messagesByPoll, cookieGenerator, cts.Token);
    }

    private async Task StartServerPollingPrivateAsync(TimeSpan delayBetweenPolls, int messagesByPoll, ValueTask<ulong>? cookieGenerator, CancellationToken cancellationToken)
    {
        IsPolling = true;

        if (!IsRunning)
            _ = StartListeningAsync(delayBetweenPolls, cancellationToken);

        await Task.Delay(300, cancellationToken);
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ulong? cookie = null;

                if (cookieGenerator is not null)
                    cookie = await cookieGenerator.Value;

                for (int i = 0; i < messagesByPoll; i++)
                    await SendPollingMessageAsync(cookie, cancellationToken);

                await Task.Delay(delayBetweenPolls, cancellationToken);
            }
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
        }

        IsPolling = false;

        await StopListeningAsync();
    }

    public async Task SendPollingMessageAsync(ulong? cookie = null, CancellationToken cancellationToken = default)
    {
        Memory<byte> message = new byte[5 + 8];
        BinaryPrimitives.TryWriteUInt16LittleEndian(message.Span, ProtocolMagic);
        message.Span[2] = (byte)FactoryServerUdpMessageType.PollServerState;
        message.Span[3] = ProtocolVersion;
        if (!cookie.HasValue)
            cookie = (ulong)_tProvider.GetUtcNow().Ticks;

        BinaryPrimitives.TryWriteUInt64LittleEndian(message.Span.Slice(4, 8), cookie.Value);

        message.Span[^1] = TerminatorByte;

        try
        {
            _sentCookies.Add(cookie.Value);
            await _client.SendAsync(message, _serverEndPoint, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            OnErrorOccurred(ex);
        }
    }

    protected virtual void OnServerStateReceived(FactoryServerStateResponse e)
    {
        ServerStateReceived?.Invoke(this, e);
    }

    protected virtual void OnErrorOccurred(Exception e)
    {
        ErrorOccurred?.Invoke(this, e);
    }
}
