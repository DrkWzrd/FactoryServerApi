using Microsoft.Extensions.Options;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace FactoryServerApi.Udp;

internal class FactoryServerUdpClient : IFactoryServerUdpClient
{
    private readonly TimeProvider _tProvider;
    private readonly UdpClient _client;
    private readonly IPEndPoint _serverEndPoint;
    private readonly UdpOptions _options;

    private CancellationTokenSource _pollingStopTokenSource;

    private readonly ConcurrentDictionary<ulong, bool> _sentCookies;

    private bool IsListening { get; set; }

    private bool IsRunning { get; set; }

    public event EventHandler<FactoryServerStateUdpResponse>? ServerStateReceived;
    public event EventHandler<Exception>? ErrorOccurred;

    public FactoryServerUdpClient(IPEndPoint serverEndPoint, TimeProvider timeProvider, IOptions<UdpOptions> options)
    {
        _client = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
        _serverEndPoint = serverEndPoint;
        _tProvider = timeProvider;
        _sentCookies = [];
        _pollingStopTokenSource = new CancellationTokenSource();
        _options = options.Value;
    }

    public Task StartPollingAsync(TimeSpan duration = default, ValueTask<ulong>? cookieGenerator = null, CancellationToken cancellationToken = default)
    {
        if (IsRunning)
            return Task.CompletedTask;

        if (_pollingStopTokenSource.IsCancellationRequested || !_pollingStopTokenSource.TryReset())
            _pollingStopTokenSource = new CancellationTokenSource();

        var resultCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _pollingStopTokenSource.Token);

        if (duration > TimeSpan.Zero)
        {
            var durationCTS = new CancellationTokenSource(duration);
            resultCTS = CancellationTokenSource.CreateLinkedTokenSource(resultCTS.Token, durationCTS.Token);
        }
        return StartPollingPrivateAsync(cookieGenerator, resultCTS.Token);
    }

    public Task StopPollingAsync()
    {
        if (!IsRunning)
            return Task.CompletedTask;

        _sentCookies.Clear();
        return _pollingStopTokenSource.CancelAsync();
    }

    private async Task StartPollingPrivateAsync(ValueTask<ulong>? cookieGenerator, CancellationToken cancellationToken = default)
    {
        IsRunning = true;

        if (!IsListening)
            _ = StartListeningPrivateAsync(cancellationToken);

        await Task.Delay(500, cancellationToken);
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ulong? cookie = null;

                if (cookieGenerator is not null)
                    cookie = await cookieGenerator.Value;

                for (int i = 0; i < _options.MessagesPerPoll; i++)
                    await SendPollingMessageAsync(cookie, cancellationToken);

                await Task.Delay(_options.DelayBetweenPolls, cancellationToken);
            }
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {

        }

        IsRunning = false;
    }

    private async Task StartListeningPrivateAsync(CancellationToken cancellationToken = default)
    {
        IsListening = true;
        var currentTry = 1;
        while (!cancellationToken.IsCancellationRequested)
        {
            var timeoutCTS = new CancellationTokenSource(_options.DelayBetweenPolls + TimeSpan.FromSeconds(1));
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                UdpReceiveResult result = await _client.ReceiveAsync(timeoutCTS.Token);

                var receivedUtc = _tProvider.GetUtcNow();
                Memory<byte> receivedData = result.Buffer;

                if (receivedData.Length < 22 || receivedData.Span[^1] != _options.MessageTermination)
                {
                    ErrorOccurred?.Invoke(this, new InvalidDataException("Invalid server udp response"));
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();

                //filter bad responses
                if (BinaryPrimitives.ReadUInt16LittleEndian(receivedData.Span[..2]) != _options.ProtocolMagic
                    || receivedData.Span[2] != (byte)FactoryServerUdpMessageType.ServerStateResponse
                    || receivedData.Span[3] != _options.ProtocolVersion
                    || receivedData.Span[^1] != _options.MessageTermination)
                {
                    ErrorOccurred?.Invoke(this, new InvalidDataException("Invalid server udp response."));
                    continue;
                }

                var cookie = BinaryPrimitives.ReadUInt64LittleEndian(receivedData.Span.Slice(4, 8));

                //filter polls we didn't do, weird and maybe impossible, but...
                if (!_sentCookies.Remove(cookie, out _))
                    continue;

                var serverStateResponse = FactoryServerStateUdpResponse.Parse(receivedData.Span[4..^1], receivedUtc);
                ServerStateReceived?.Invoke(this, serverStateResponse);
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == timeoutCTS.Token)
            {
                if (currentTry > _options.TimeoutRetriesBeforeStop)
                {
                    ErrorOccurred?.Invoke(this, new TimeoutException($"More than {_options.TimeoutRetriesBeforeStop} tries ({(_options.DelayBetweenPolls * currentTry):ss} seconds) without server response."));
                    break;
                }
                currentTry++;
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {

            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
            }
        }
        IsListening = false;
    }

    private async Task SendPollingMessageAsync(ulong? cookie = null, CancellationToken cancellationToken = default)
    {
        Memory<byte> message = new byte[5 + 8];
        BinaryPrimitives.TryWriteUInt16LittleEndian(message.Span, _options.ProtocolMagic);
        message.Span[2] = (byte)FactoryServerUdpMessageType.PollServerState;
        message.Span[3] = _options.ProtocolVersion;
        if (!cookie.HasValue)
            cookie = (ulong)_tProvider.GetUtcNow().Ticks;

        BinaryPrimitives.TryWriteUInt64LittleEndian(message.Span.Slice(4, 8), cookie.Value);

        message.Span[^1] = _options.MessageTermination;

        try
        {
            _sentCookies.TryAdd(cookie.Value, true);
            await _client.SendAsync(message, _serverEndPoint, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorOccurred?.Invoke(this, ex);
        }
    }
}
