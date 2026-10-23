using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;


namespace FactoryServerApi.Udp;

internal class FactoryServerUdpClient : IFactoryServerUdpClient, IDisposable
{
    private readonly TimeProvider _tProvider;
    private readonly UdpClient _client;
    private readonly IPEndPoint _serverEndPoint;
    private readonly UdpOptions _options;

    private static readonly TimeSpan CookieTtl = TimeSpan.FromSeconds(60);

    private CancellationTokenSource _pollingStopTokenSource = new();
    private readonly ConcurrentDictionary<ulong, DateTimeOffset> _sentCookies = [];
    private long _pollCookieCounter;

    private volatile bool _isListening;
    private volatile bool _isRunning;

    public event EventHandler<FactoryServerStateUdpResponse>? ServerStateReceived;
    public event EventHandler<Exception>? ErrorOccurred;

    public FactoryServerUdpClient(IPEndPoint serverEndPoint, TimeProvider timeProvider, IOptions<UdpOptions> options)
    {
        _client = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
        _serverEndPoint = serverEndPoint;
        _tProvider = timeProvider;
        _options = options.Value;
    }

    public Task StartPollingAsync(TimeSpan duration = default, ICookieGenerator? cookieGenerator = null, CancellationToken cancellationToken = default)
    {
        if (_isRunning)
            return Task.CompletedTask;

        if (_pollingStopTokenSource.IsCancellationRequested || !_pollingStopTokenSource.TryReset())
            _pollingStopTokenSource = new CancellationTokenSource();

        CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _pollingStopTokenSource.Token);

        if (duration > TimeSpan.Zero)
        {
            CancellationTokenSource durationCts = new(duration);
            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(linkedCts.Token, durationCts.Token);
        }

        return StartPollingPrivateAsync(cookieGenerator, linkedCts);
    }

    public Task StopPollingAsync()
    {
        if (!_isRunning)
            return Task.CompletedTask;

        _sentCookies.Clear();
        return _pollingStopTokenSource.CancelAsync();
    }

    private async Task StartPollingPrivateAsync(ICookieGenerator? cookieGenerator, CancellationTokenSource linkedCts)
    {
        CancellationToken ct = linkedCts.Token;
        _isRunning = true;

        if (!_isListening)
            _ = StartListeningPrivateAsync(linkedCts);

        await Task.Delay(500, ct);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                ulong? cookie = null;

                if (cookieGenerator is not null)
                    cookie = await cookieGenerator.GetCookieAsync(_tProvider);

                for (int i = 0; i < _options.MessagesPerPoll; i++)
                    await SendPollingMessageAsync(cookie, ct);

                await Task.Delay(_options.DelayBetweenPolls, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // graceful cancel
        }
        finally
        {
            _isRunning = false;
        }
    }

    private async Task StartListeningPrivateAsync(CancellationTokenSource cts)
    {
        CancellationToken ct = cts.Token;
        _isListening = true;
        int attempt = 1;

        while (!ct.IsCancellationRequested)
        {
            CancellationTokenSource timeoutCts = new(_options.DelayBetweenPolls + TimeSpan.FromSeconds(1));

            try
            {
                await ReceiveMessagePrivateAsync(timeoutCts, false, ct);
                attempt = 1; // reset retry counter after success
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == timeoutCts.Token)
            {
                if (attempt++ > _options.TimeoutRetriesBeforeStop)
                {
                    EmitError(new TimeoutException($"No server response after {attempt - 1} attempts (~{(_options.DelayBetweenPolls * attempt).TotalSeconds:N0}s)."));
                    // The server is unreachable: stop the poll loop as well
                    // to avoid pinging a dead server indefinitely (anti-DDoS).
                    cts.Cancel();
                    break;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // graceful shutdown
            }
            catch (Exception ex)
            {
                EmitError(ex);
            }
        }

        _isListening = false;
    }

    public Task ReceiveMessageAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        CancellationTokenSource timeoutCts = new(timeout + TimeSpan.FromSeconds(1));
        return ReceiveMessagePrivateAsync(timeoutCts, true, ct);
    }

    private async Task ReceiveMessagePrivateAsync(CancellationTokenSource timeoutCts, bool throwEx, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        UdpReceiveResult result = await _client.ReceiveAsync(timeoutCts.Token);
        Memory<byte> data = result.Buffer;
        DateTimeOffset now = _tProvider.GetUtcNow();

        if (data.Length < 22 || data.Span[^1] != _options.MessageTermination)
        {
            HandleInvalid(throwEx, "Too short or bad termination");
            return;
        }

        Span<byte> span = data.Span;

        if (BinaryPrimitives.ReadUInt16LittleEndian(span) != _options.ProtocolMagic
            || span[2] != (byte)FactoryServerUdpMessageType.ServerStateResponse
            || span[3] != _options.ProtocolVersion)
        {
            HandleInvalid(throwEx, "Bad header");
            return;
        }

        ulong cookie = BinaryPrimitives.ReadUInt64LittleEndian(span.Slice(4, 8));

        if (!_sentCookies.TryRemove(cookie, out DateTimeOffset sentAt) || now - sentAt > CookieTtl)
        {
            HandleInvalid(throwEx, "Unexpected cookie");
            return;
        }

        try
        {
            FactoryServerStateUdpResponse response = FactoryServerStateUdpResponse.Deserialize(span[4..^1], now);
            if (!throwEx)
                ServerStateReceived?.Invoke(this, response);
        }
        catch
        {
            HandleInvalid(throwEx, "Deserialization failed");
        }

        void HandleInvalid(bool shouldThrow, string reason)
        {
            InvalidDataException ex = new($"Invalid UDP response: {reason}");
            if (shouldThrow)
                throw ex;
            EmitError(ex);
        }
    }

    public async Task SendPollingMessageAsync(ulong? cookie = null, CancellationToken ct = default)
    {
        byte[] messageRaw = ArrayPool<byte>.Shared.Rent(5 + 8);
        Memory<byte> message = messageRaw.AsMemory()[..(5 + 8)];

        Span<byte> messageSpan = message.Span;

        BinaryPrimitives.WriteUInt16LittleEndian(messageSpan, _options.ProtocolMagic);
        messageSpan[2] = (byte)FactoryServerUdpMessageType.PollServerState;
        messageSpan[3] = _options.ProtocolVersion;

        cookie ??= (ulong)Interlocked.Increment(ref _pollCookieCounter);
        BinaryPrimitives.WriteUInt64LittleEndian(messageSpan.Slice(4, 8), cookie.Value);
        messageSpan[^1] = _options.MessageTermination;

        try
        {
            PruneStaleCookies();
            _sentCookies.TryAdd(cookie.Value, _tProvider.GetUtcNow());
            await _client.SendAsync(message, _serverEndPoint, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            EmitError(ex);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(messageRaw);
        }
    }

    private void PruneStaleCookies()
    {
        DateTimeOffset now = _tProvider.GetUtcNow();
        foreach (KeyValuePair<ulong, DateTimeOffset> sentCookie in _sentCookies)
            if (now - sentCookie.Value > CookieTtl)
                _sentCookies.TryRemove(sentCookie.Key, out _);
    }

    private void EmitError(Exception ex) => ErrorOccurred?.Invoke(this, ex);

    public void Dispose() => _client.Dispose();
}
