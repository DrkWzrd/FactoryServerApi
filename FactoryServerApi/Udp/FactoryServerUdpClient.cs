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

    public event EventHandler<FactoryServerStateResponse>? ServerStateReceived;
    public event EventHandler<Exception>? ErrorOccurred;

    public FactoryServerUdpClient(IPEndPoint serverEndPoint, TimeProvider timeProvider)
    {
        _client = new UdpClient(new IPEndPoint(IPAddress.Any, 7777));
        _serverEndPoint = serverEndPoint;
        _tProvider = timeProvider;
        _sentCookies = [];
        _listenerStopTokenSource = new CancellationTokenSource();
    }

    public async Task PollServerStateAsync(ulong? cookie = null, CancellationToken cancellationToken = default)
    {
        byte[] message = new byte[5 + 8];
        BinaryPrimitives.TryWriteUInt16LittleEndian(message, ProtocolMagic);
        message[2] = (byte)FactoryServerUdpMessageType.PollServerState;
        message[3] = ProtocolVersion;
        if (!cookie.HasValue)
            cookie = (ulong)_tProvider.GetUtcNow().Ticks;

        BinaryPrimitives.TryWriteUInt64LittleEndian(message.AsSpan().Slice(4, 8), cookie.Value);

        message[^1] = TerminatorByte;

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

    public async Task PollServerStateAsync(TimeSpan pollingDuration, TimeSpan delayBetweenPolls, bool repeatPoll = false, int messagesByPoll = 1, Func<ulong>? cookieGenerator = null, CancellationToken cancellationToken = default)
    {
        var pollingCTS = new CancellationTokenSource(pollingDuration);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, pollingCTS.Token);

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var pollsCount = repeatPoll ? messagesByPoll : 1;

                for (int i = 0; i < messagesByPoll; i++)
                {
                    await PollServerStateAsync(cookieGenerator?.Invoke(), cts.Token);
                }

                await Task.Delay(delayBetweenPolls, cts.Token);
            }
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cts.Token)
        {

        }
    }

    private async Task StartListeningPrivateAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                UdpReceiveResult result = await _client.ReceiveAsync(cancellationToken);

                byte[] receivedData = result.Buffer;

                if (receivedData.Length < 22 || receivedData[^1] != TerminatorByte)
                    throw new InvalidDataException("Invalid response format");

                if (cancellationToken.IsCancellationRequested)
                    break;

                //filter bad responses
                if (BinaryPrimitives.ReadUInt16LittleEndian(receivedData.AsSpan(0, 2)) != ProtocolMagic
                    || receivedData[2] != (byte)FactoryServerUdpMessageType.ServerStateResponse
                    || receivedData[3] != ProtocolVersion
                    || receivedData[^1] != TerminatorByte)
                    continue;

                var cookie = BinaryPrimitives.ReadUInt64LittleEndian(receivedData.AsSpan().Slice(4, 8));

                //filter polls we didn't do
                if (!_sentCookies.Remove(cookie))
                    continue;

                var serverStateResponse = FactoryServerStateResponse.Parse(receivedData.AsSpan()[4..^1]);
                OnServerStateReceived(serverStateResponse);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
            }
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

    public Task StartListeningAsync(CancellationToken cancellationToken = default)
    {
        if (_listenerStopTokenSource.IsCancellationRequested || !_listenerStopTokenSource.TryReset())
            _listenerStopTokenSource = new CancellationTokenSource();

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _listenerStopTokenSource.Token);
        return StartListeningPrivateAsync(cts.Token);
    }

    public Task StopListeningAsync()
    {
        _sentCookies.Clear();
        return _listenerStopTokenSource.CancelAsync();
    }
}
