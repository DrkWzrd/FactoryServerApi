using FactoryServerApi.Http;
using FactoryServerApi.Http.Responses;
using FactoryServerApi.Udp;
using System.Collections.Concurrent;

namespace FactoryServerApi;

public record FactoryServerManagerOptions(string IpAdressOrUrl, int Port, Func<FactoryServerManager, string?> AuthTokenProvider, TimeSpan DelayBetweenPolls, int MessagesPerPingCount = 1);

public abstract class FactoryServerManager : IFactoryServerManager
{

    private readonly IFactoryServerUdpClient _udpClient;
    private readonly IFactoryServerHttpService _httpService;
    private readonly IFactoryServerApi _serverApi;
    private readonly ConcurrentQueue<FactoryServerSubState> _statesToQueryQueue;
    private readonly ConcurrentDictionary<FactoryServerSubStateId, ulong> _statesToQueryUniquenessGuard;
    private readonly SemaphoreSlim _currentServerSemaphore;

    private readonly Dictionary<FactoryServerSubStateId, ushort> _cachedSubStatesVersions;

    public event EventHandler<FactoryServerStateResponse>? ServerStateReceived
    {
        add { _udpClient.ServerStateReceived += value; }
        remove { _udpClient.ServerStateReceived -= value; }
    }
    public event EventHandler<Exception>? ErrorOccurred
    {
        add { _udpClient.ErrorOccurred += value; }
        remove { _udpClient.ErrorOccurred -= value; }
    }

    public FactoryServerManagerOptions Options { get; }

    public FactoryServerInfo? _currentServerInfo;

    public async Task<FactoryServerInfo?> GetCurrentServerInfoAsync()
    {
        await _currentServerSemaphore.WaitAsync();
        try
        {
            return _currentServerInfo;
        }
        finally
        {
            _currentServerSemaphore.Release();
        }
    }

    public IFactoryServerApi CurrentServerApi => _serverApi;

    private async Task<FactoryServerState?> GetCurrentStateAsync()
    {
        if (_currentServerInfo is null)
            return null;
        await _currentServerSemaphore.WaitAsync();
        try
        {
            return _currentServerInfo.ServerState;
        }
        finally
        {
            _currentServerSemaphore.Release();
        }
    }

    private async Task UpdateCurrentStateAsync(FactoryServerStateResponse response)
    {
        if (_currentServerInfo is null)
            throw new InvalidOperationException();

        await _currentServerSemaphore.WaitAsync();
        try
        {
            var health = await _httpService.HealthCheckAsync(null);
            if (health.Result is null)
                throw new InvalidOperationException();

            _currentServerInfo.UpdateValue(response, health.Result);
        }
        finally
        {
            _currentServerSemaphore.Release();
        }
    }

    public FactoryServerManager(IFactoryServerUdpClient udpClient, IFactoryServerApi serverApi, FactoryServerManagerOptions options)
    {
        Options = options;
        _udpClient = udpClient;
        _serverApi = serverApi;
        _httpService = serverApi;
        _statesToQueryQueue = [];
        _statesToQueryUniquenessGuard = [];
        _cachedSubStatesVersions = [];
        _currentServerSemaphore = new(1, 1);
        _udpClient.ServerStateReceived += InternalServerStateHandler;
    }

    public Task ConnectToServerAsync(CancellationToken cancellationToken = default)
    {
        _ = ProcessChangedSubStatesQueue(cancellationToken);
        _ = _udpClient.StartServerPollingAsync(Options.DelayBetweenPolls, Options.MessagesPerPingCount, null, cancellationToken);

        return Task.CompletedTask;
    }

    protected virtual ValueTask<bool> HandleCustomSubStateAsync(FactoryServerInfo currentServer, FactoryServerSubState subState, ulong cookie, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(true);
    }

    private async void InternalServerStateHandler(object? sender, FactoryServerStateResponse e)
    {
        try
        {
            if (_currentServerInfo is null)
            {
                await HandleFirstServerResponseAsync(e);
            }
            else
            {
                if (e.ServerNetCL != _currentServerInfo.ChangeList)
                    throw new InvalidOperationException();

                //if (e.Cookie == _currentServerInfo.LastSeenCookie)
                //    return;

                await UpdateCurrentStateAsync(e);

                var subStates = e.SubStates;
                IEnumerable<FactoryServerSubState> changedSubStates = _cachedSubStatesVersions.Count == 0
                    ? subStates
                    : subStates.Where(sS => !_cachedSubStatesVersions.ContainsKey(sS.SubStateId) || sS.SubStateVersion != _cachedSubStatesVersions[sS.SubStateId]);

                foreach (var subState in changedSubStates)
                    EnqueueSubStateQuery(subState, e.Cookie);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private void EnqueueSubStateQuery(FactoryServerSubState subState, ulong cookie)
    {
        //if there is a pending request doesn't add new one
        if (_statesToQueryUniquenessGuard.TryAdd(subState.SubStateId, cookie))
        {
            _statesToQueryQueue.Enqueue(subState);
        }
        else
        {
            _statesToQueryUniquenessGuard[subState.SubStateId] = cookie;
        }
    }

    private async Task HandleFirstServerResponseAsync(FactoryServerStateResponse e, CancellationToken cancellationToken = default)
    {
        await _currentServerSemaphore.WaitAsync();
        try
        {
            var health = await _httpService.HealthCheckAsync(null);
            if (health.Result is null)
                throw new NotImplementedException();

            var serverGameState = await _httpService.QueryServerStateAsync();
            if (serverGameState.Result is null)
                throw new NotImplementedException();

            var options = await _httpService.GetServerOptionsAsync();
            if (options.Result is null)
                throw new NotImplementedException();

            var advancedSettings = await _httpService.GetAdvancedGameSettingsAsync();
            if (advancedSettings.Result is null)
                throw new NotImplementedException();

            var sessions = await _httpService.EnumerateSessionsAsync();
            if (sessions.Result is null)
                throw new NotImplementedException();

            _currentServerInfo = new FactoryServerInfo(
                e.ServerName,
                //e.Cookie,
                e.ServerState,
                e.ServerFlags,
                e.ServerNetCL,
                health.Result.Health,
                health.Result.ServerCustomData,
                serverGameState.Result.ServerGameState,
                options.Result.ServerOptions,
                options.Result.PendingServerOptions,
                advancedSettings.Result.CreativeModeEnabled,
                advancedSettings.Result.AdvancedGameSettings,
                sessions.Result.Sessions,
                sessions.Result.CurrentSessionIndex);
        }
        finally
        {
            _currentServerSemaphore.Release();
        }
    }

    private async Task ProcessChangedSubStatesQueue(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var serverState = await GetCurrentStateAsync();
            if (serverState == FactoryServerState.Loading || _statesToQueryQueue.IsEmpty)
            {
                await Task.Delay(1000, cancellationToken);
            }
            else if (_statesToQueryQueue.TryDequeue(out var subState))
            {
                await ProcessChangedSubStateAsync(subState, _statesToQueryUniquenessGuard[subState.SubStateId], cancellationToken);
            }
        }
    }

    private async Task ProcessChangedSubStateAsync(FactoryServerSubState subState, ulong cookie, CancellationToken cancellationToken = default)
    {
        switch (subState.SubStateId)
        {
            case FactoryServerSubStateId.ServerGameState:
                await ProcessSubStateAsync(subState, cookie, httpService => httpService.QueryServerStateAsync(), cancellationToken);
                break;
            case FactoryServerSubStateId.ServerOptions:
                await ProcessSubStateAsync(subState, cookie, httpService => httpService.GetServerOptionsAsync(), cancellationToken);
                break;
            case FactoryServerSubStateId.AdvancedGameSettings:
                await ProcessSubStateAsync(subState, cookie, httpService => httpService.GetAdvancedGameSettingsAsync(), cancellationToken);
                break;
            case FactoryServerSubStateId.SaveCollection:
                await ProcessSubStateAsync(subState, cookie, httpService => httpService.EnumerateSessionsAsync(), cancellationToken);
                break;
            default:
                if (_currentServerInfo == null)
                    throw new InvalidOperationException();

                var gracefullyManaged = await HandleCustomSubStateAsync(_currentServerInfo, subState, cookie, cancellationToken);
                if (gracefullyManaged)
                    UpdateCacheState(subState);
                break;
        }
    }

    private async Task ProcessSubStateAsync<T>(
        FactoryServerSubState subState,
        ulong cookie,
        Func<IFactoryServerHttpService, Task<(T? Result, FactoryServerError? Error)>> queryFunc,
        CancellationToken cancellationToken)
    {
        var httpService = await GetHttpServiceAsync(cancellationToken);
        var result = await queryFunc(httpService);

        if (result.Result is null)
            throw new NotImplementedException();

        UpdateServerInfo(subState, result.Result, cookie);
    }

    //private async Task ProcessChangedSubStateAsync(FactoryServerSubState subState, ulong cookie, CancellationToken cancellationToken = default)
    //{
    //    if (subState.SubStateId == FactoryServerSubStateId.ServerGameState)
    //    {
    //        var httpService = await GetHttpServiceAsync(cancellationToken);
    //        var serverGameState = await httpService.QueryServerStateAsync();
    //        if (serverGameState.Result is null)
    //            throw new NotImplementedException();

    //        UpdateServerInfo(subState, serverGameState.Result, cookie);
    //    }
    //    else if (subState.SubStateId == FactoryServerSubStateId.ServerOptions)
    //    {
    //        var httpService = await GetHttpServiceAsync(cancellationToken);
    //        var options = await httpService.GetServerOptionsAsync();
    //        if (options.Result is null)
    //            throw new NotImplementedException();

    //        UpdateServerInfo(subState, options.Result, cookie);
    //    }
    //    else if (subState.SubStateId == FactoryServerSubStateId.AdvancedGameSettings)
    //    {
    //        var httpService = await GetHttpServiceAsync(cancellationToken);
    //        var advancedSettings = await httpService.GetAdvancedGameSettingsAsync();
    //        if (advancedSettings.Result is null)
    //            throw new NotImplementedException();

    //        UpdateServerInfo(subState, advancedSettings.Result, cookie);
    //    }
    //    else if (subState.SubStateId == FactoryServerSubStateId.SaveCollection)
    //    {
    //        var httpService = await GetHttpServiceAsync(cancellationToken);
    //        var sessions = await httpService.EnumerateSessionsAsync();
    //        if (sessions.Result is null)
    //            throw new NotImplementedException();

    //        UpdateServerInfo(subState, sessions.Result, cookie);
    //    }
    //    else
    //    {
    //        if (_currentServerInfo is null)
    //            throw new InvalidOperationException();

    //        var gracefullyManaged = await HandleCustomSubStateAsync(_currentServerInfo, subState, cookie, cancellationToken);
    //        if (gracefullyManaged)
    //            UpdateCacheState(subState);
    //    }

    //}

    private async Task<IFactoryServerHttpService> GetHttpServiceAsync(CancellationToken token = default)
    {
        await _currentServerSemaphore.WaitAsync(token);
        try
        {
            return _httpService;
        }
        finally
        {
            _currentServerSemaphore.Release();
        }
    }

    private void UpdateServerInfo<T>(FactoryServerSubState handledSubState, T result, ulong cookie)
    {
        if (_currentServerInfo is null)
            throw new InvalidOperationException();

        //if everything is ok we update the cached version
        if (result is QueryServerStateResponseData stateData)
        {
            _currentServerInfo.GameState = stateData.ServerGameState;
        }
        else if (result is GetServerOptionsResponseData optionsData)
        {
            _currentServerInfo.UpdateValue(optionsData, cookie);
        }
        else if (result is GetAdvancedGameSettingsResponseData advancedSettingsData)
        {
            _currentServerInfo.UpdateValue(advancedSettingsData, cookie);
        }
        else if (result is EnumerateSessionsResponseData sessionsData)
        {
            _currentServerInfo.UpdateValue(sessionsData, cookie);
        }
        UpdateCacheState(handledSubState);
    }

    private void UpdateCacheState(FactoryServerSubState handledSubState)
    {
        if (_statesToQueryUniquenessGuard.TryRemove(handledSubState.SubStateId, out _))
        {
            //if everything is fine and the guard is removed, we update the cached version value
            _cachedSubStatesVersions.Add(handledSubState.SubStateId, handledSubState.SubStateVersion);
        }
        else
        {
            //if something is wrong trying to remove the guard we have to enqueue again a query
            //to retry the query and avoid the corrupt state
            _statesToQueryQueue.Enqueue(handledSubState);
        }
    }
}
