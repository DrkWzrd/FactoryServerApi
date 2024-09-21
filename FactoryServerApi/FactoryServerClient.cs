using FactoryServerApi.Http;
using FactoryServerApi.Http.Responses;
using FactoryServerApi.Udp;
using System.Collections.Concurrent;

namespace FactoryServerApi;

internal class FactoryServerClient : IFactoryServerClient
{

    private readonly IFactoryServerUdpClient _udpClient;
    private readonly IFactoryServerHttpClient _httpClient;
    private readonly ConcurrentQueue<FactoryServerSubState> _statesToRequestQueue;
    private readonly ConcurrentDictionary<FactoryServerSubStateId, ulong> _subStateIdsQueriesUniquenessGuard;
    private readonly SemaphoreSlim _currentServerStateSemaphore;

    private readonly Dictionary<FactoryServerSubStateId, ushort> _cachedSubStatesVersions;

    private FactoryServerGlobalState _serverInfo = FactoryServerGlobalState.Offline;

    public event EventHandler<FactoryServerErrorEventArgs>? ErrorOccurred;
    public event EventHandler<FactoryServerStateChangedEventArgs>? ServerStateChanged;

    public bool IsServerClaimed => _httpClient.IsServerClaimed;

    public FactoryServerPrivilegeLevel CurrentPrivilegeLevel => _httpClient.AuthenticationData.TokenPrivilegeLevel;

    public FactoryServerStateSnapshot CurrentServerState => GetCurrentServerState();

    private FactoryServerStateSnapshot GetCurrentServerState()
    {
        _currentServerStateSemaphore.Wait();
        try
        {
            return _serverInfo.GetSnapshot();
        }
        finally
        {
            _currentServerStateSemaphore.Release();
        }
    }

    public FactoryServerClient(IFactoryServerUdpClient udpClient, IFactoryServerHttpClient httpClient)
    {
        _udpClient = udpClient;
        _httpClient = httpClient;
        _currentServerStateSemaphore = new(1, 1);
        _statesToRequestQueue = [];
        _subStateIdsQueriesUniquenessGuard = [];
        _cachedSubStatesVersions = [];
        _udpClient.ServerStateReceived += InternalServerStateHandler;
        _udpClient.ErrorOccurred += UdpClient_ErrorOccurred;
    }

    private void UdpClient_ErrorOccurred(object? sender, Exception e)
    {
        var args = new FactoryServerErrorEventArgs(e);
        ErrorOccurred?.Invoke(this, args);
    }

    public Task StartConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (!_httpClient.IsServerClaimed)
        {
            ErrorOccurred?.Invoke(this, new FactoryServerErrorEventArgs(new InvalidOperationException("The server must be claimed before run this client")));
            return Task.CompletedTask;
        }
        _ = ProcessChangedSubStatesQueue(cancellationToken);
        _ = _udpClient.StartPollingAsync(cancellationToken: cancellationToken);
        return Task.CompletedTask;
    }

    private async Task ProcessChangedSubStatesQueue(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var serverState = GetCurrentServerState().ServerState;

            if (serverState != FactoryServerState.Loading
                && serverState != FactoryServerState.Offline
                && _statesToRequestQueue.TryDequeue(out var subState))
            {
                await ProcessChangedSubStateAsync(subState, cancellationToken);
            }
            else
            {
                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    private async void InternalServerStateHandler(object? sender, FactoryServerStateUdpResponse e)
    {
        if (_serverInfo == FactoryServerGlobalState.Offline)
        {
            await HandleFirstServerResponseAsync(e);
            ServerStateChanged?.Invoke(this, new FactoryServerStateChangedEventArgs(_serverInfo));
        }
        else
        {
            if (e.ServerNetCL != _serverInfo.ChangeList)
            {
                ErrorOccurred?.Invoke(this, new FactoryServerErrorEventArgs(new NotSupportedException("Server version and client version differs.")));
                return;
            }

            await UpdateCurrentStateAsync(e);

            var subStates = e.SubStates;
            IEnumerable<FactoryServerSubState> changedSubStates = _cachedSubStatesVersions.Count == 0
                ? subStates
                : subStates.Where(sS => !_cachedSubStatesVersions.ContainsKey(sS.SubStateId) || sS.SubStateVersion != _cachedSubStatesVersions[sS.SubStateId]);

            foreach (var subState in changedSubStates)
                EnqueueSubStateQuery(subState, e.Cookie);
        }
    }

    private async Task UpdateCurrentStateAsync(FactoryServerStateUdpResponse response, CancellationToken cancellationToken = default)
    {
        await _currentServerStateSemaphore.WaitAsync(cancellationToken);
        try
        {
            var health = await _httpClient.HealthCheckAsync(null, cancellationToken: cancellationToken);
            if (HandleResponseContentErrors(health))
                throw new InvalidOperationException("Something happened to http server.");

            _serverInfo.UpdateValue(response, health.Data!);
            ServerStateChanged?.Invoke(this, new FactoryServerStateChangedEventArgs(_serverInfo));
        }
        finally
        {
            _currentServerStateSemaphore.Release();
        }
    }

    private void EnqueueSubStateQuery(FactoryServerSubState subState, ulong cookie)
    {
        //if there is a pending request of the same type don't add new one
        if (_subStateIdsQueriesUniquenessGuard.TryAdd(subState.SubStateId, cookie))
            _statesToRequestQueue.Enqueue(subState);
        else
            _subStateIdsQueriesUniquenessGuard[subState.SubStateId] = cookie;
    }

    private async Task HandleFirstServerResponseAsync(FactoryServerStateUdpResponse e, CancellationToken cancellationToken = default)
    {
        await _currentServerStateSemaphore.WaitAsync(cancellationToken);
        try
        {
            FactoryServerHealthState healthState = FactoryServerHealthState.Healthy;
            string? serverCustomData = null;
            ServerGameState gameState;
            IReadOnlyDictionary<string, string> currentOptions = new Dictionary<string, string>();
            IReadOnlyDictionary<string, string> pendingOptions = new Dictionary<string, string>();
            bool creativeModeEnabled = false;
            IReadOnlyDictionary<string, string> advancedSettings = new Dictionary<string, string>(); ;
            IReadOnlyList<SessionSaveStruct> sessions = [];
            int currentSessionIndex = -1;
            var health = await _httpClient.HealthCheckAsync(null, cancellationToken: cancellationToken);

            if (HandleResponseContentErrors(health))
                throw new InvalidOperationException("Something happened to http server.");

            healthState = health.Data!.Health;
            serverCustomData = health.Data.ServerCustomData;

            var serverGameState = await _httpClient.QueryServerStateAsync(cancellationToken: cancellationToken);

            gameState = !HandleResponseContentErrors(serverGameState)
                ? serverGameState.Data!.ServerGameState
                : ServerGameState.Unknown;

            var options = await _httpClient.GetServerOptionsAsync(cancellationToken: cancellationToken);
            if (!HandleResponseContentErrors(options))
            {
                currentOptions = options.Data!.ServerOptions;
                pendingOptions = options.Data.PendingServerOptions;
            }

            var advancedSettingsData = await _httpClient.GetAdvancedGameSettingsAsync(cancellationToken: cancellationToken);
            if (!HandleResponseContentErrors(advancedSettingsData))
            {
                creativeModeEnabled = advancedSettingsData.Data!.CreativeModeEnabled;
                advancedSettings = advancedSettingsData.Data.AdvancedGameSettings;
            }

            var sessionsData = await _httpClient.EnumerateSessionsAsync(cancellationToken: cancellationToken);
            if (!HandleResponseContentErrors(sessionsData))
            {
                sessions = sessionsData.Data!.Sessions;
                currentSessionIndex = sessionsData.Data.CurrentSessionIndex;
            }

            _serverInfo = new FactoryServerGlobalState(
                        e.ServerName, e.ServerState, e.ServerFlags, e.ServerNetCL,
                        healthState, serverCustomData,
                        gameState,
                        currentOptions, pendingOptions,
                        creativeModeEnabled, advancedSettings,
                        sessions, currentSessionIndex
                    );
        }
        finally
        {
            _currentServerStateSemaphore.Release();
        }
    }

    private bool HandleResponseContentErrors<T>(FactoryServerResponseContent<T> content)
        where T : FactoryServerResponseContentData
    {
        if (content.Data is not null)
            return false;
        else if (content.Error is not null)
            ErrorOccurred?.Invoke(this, new FactoryServerErrorEventArgs(content.Error));
        else
            ErrorOccurred?.Invoke(this, new FactoryServerErrorEventArgs(new InvalidDataException($"Something went wrong trying to retrieve {typeof(T)}.")));
        return true;
    }

    private async Task ProcessChangedSubStateAsync(FactoryServerSubState subState, CancellationToken cancellationToken = default)
    {
        switch (subState.SubStateId)
        {
            case FactoryServerSubStateId.ServerGameState:
                await ProcessSubStateAsync(subState, httpService => httpService.QueryServerStateAsync(), cancellationToken);
                break;
            case FactoryServerSubStateId.ServerOptions:
                await ProcessSubStateAsync(subState, httpService => httpService.GetServerOptionsAsync(), cancellationToken);
                break;
            case FactoryServerSubStateId.AdvancedGameSettings:
                await ProcessSubStateAsync(subState, httpService => httpService.GetAdvancedGameSettingsAsync(), cancellationToken);
                break;
            case FactoryServerSubStateId.SaveCollection:
                await ProcessSubStateAsync(subState, httpService => httpService.EnumerateSessionsAsync(), cancellationToken);
                break;
            default:

                var gracefullyManaged = await HandleCustomSubStateAsync(_serverInfo, subState, cancellationToken);
                if (gracefullyManaged)
                    UpdateSubStateCache(subState);
                break;
        }
    }

    protected virtual ValueTask<bool> HandleCustomSubStateAsync(FactoryServerGlobalState currentServer, FactoryServerSubState subState, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(true);
    }

    private async Task ProcessSubStateAsync<T>(FactoryServerSubState subState, Func<IFactoryServerHttpClient, Task<FactoryServerResponseContent<T>>> queryFunc, CancellationToken cancellationToken = default)
        where T : FactoryServerResponseContentData
    {
        var httpService = await GetHttpServiceAsync(cancellationToken);
        var responseContent = await queryFunc(httpService);

        if (HandleResponseContentErrors(responseContent))
            return;

        UpdateServerSubState(subState, responseContent.Data);
        ServerStateChanged?.Invoke(this, new FactoryServerStateChangedEventArgs(_serverInfo));
    }

    private async Task<IFactoryServerHttpClient> GetHttpServiceAsync(CancellationToken token = default)
    {
        await _currentServerStateSemaphore.WaitAsync(token);
        try
        {
            return _httpClient;
        }
        finally
        {
            _currentServerStateSemaphore.Release();
        }
    }

    private void UpdateServerSubState<T>(FactoryServerSubState handledSubState, T result)
    {
        //if everything is ok we update the cached version
        if (result is QueryServerStateData stateData)
        {
            _serverInfo.GameState = stateData.ServerGameState;
        }
        else if (result is GetServerOptionsData optionsData)
        {
            _serverInfo.UpdateValue(optionsData);
        }
        else if (result is GetAdvancedGameSettingsData advancedSettingsData)
        {
            _serverInfo.UpdateValue(advancedSettingsData);
        }
        else if (result is EnumerateSessionsData sessionsData)
        {
            _serverInfo.UpdateValue(sessionsData);
        }
        UpdateSubStateCache(handledSubState);
    }

    private void UpdateSubStateCache(FactoryServerSubState handledSubState)
    {
        if (_subStateIdsQueriesUniquenessGuard.TryRemove(handledSubState.SubStateId, out _))
        {
            //if everything is fine and the guard is removed, we update the cached version value
            _cachedSubStatesVersions.Add(handledSubState.SubStateId, handledSubState.SubStateVersion);
        }
        else
        {
            //if something is wrong trying to remove the guard we have to enqueue again a query
            //to retry the query and avoid the corrupt state
            _statesToRequestQueue.Enqueue(handledSubState);
        }
    }
}

public class FactoryServerStateChangedEventArgs : EventArgs
{

    public FactoryServerStateSnapshot ServerStateSnapshot { get; }

    internal FactoryServerStateChangedEventArgs(FactoryServerGlobalState serverState)
    {
        ServerStateSnapshot = serverState.GetSnapshot();
    }
}