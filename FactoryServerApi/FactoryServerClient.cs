using System.Collections.Concurrent;
using System.Net.Sockets;
using FactoryServerApi.Http;
using FactoryServerApi.Http.Responses;
using FactoryServerApi.Udp;

namespace FactoryServerApi;

internal class FactoryServerClient : IFactoryServerClient
{

    private readonly IFactoryServerUdpClient _pollingUdpClient;
    private readonly IFactoryServerUdpClient _pingUdpClient;
    private readonly IFactoryServerHttpClient _httpClient;
    private readonly ConcurrentQueue<FactoryServerSubState> _statesToRequestQueue;
    private readonly ConcurrentDictionary<FactoryServerSubStateId, ulong> _subStateIdsQueriesUniquenessGuard;
    private readonly SemaphoreSlim _currentServerStateSemaphore;
    private readonly FactoryServerInfo _serverInfo;
    private ulong _pingUdpCounter;

    private readonly Dictionary<FactoryServerSubStateId, ushort> _cachedSubStatesVersions;

    public event EventHandler<FactoryServerErrorEventArgs>? ErrorOccurred;
    public event EventHandler<FactoryServerStateChangedEventArgs>? ServerStateChanged;

    public FactoryGamePlayerId? PlayerId => _httpClient.PlayerId;

    public FactoryServerPrivilegeLevel ClientCurrentPrivilegeLevel => AuthenticationTokenHelper.GetTokenLevel(_httpClient.AuthenticationToken?.AsMemory());

    public IFactoryServerHttpClient ApiAccessPoint => _httpClient;

    public FactoryServerClient(IFactoryServerUdpClient pollClient, IFactoryServerUdpClient pingClient, IFactoryServerHttpClient httpClient)
    {
        _pollingUdpClient = pollClient;
        _pingUdpClient = pingClient;
        _httpClient = httpClient;
        _currentServerStateSemaphore = new(1, 1);
        _statesToRequestQueue = [];
        _subStateIdsQueriesUniquenessGuard = [];
        _cachedSubStatesVersions = [];
        _pollingUdpClient.ServerStateReceived += InternalServerStateHandler;
        _pollingUdpClient.ErrorOccurred += UdpClient_ErrorOccurred;
        _serverInfo = new();
    }

    public async Task<FactoryServerInfoSnapshot> GetCurrentServerStateAsync(CancellationToken cancellationToken = default)
    {
        await _currentServerStateSemaphore.WaitAsync(cancellationToken);
        try
        {
            return new FactoryServerInfoSnapshot(_serverInfo);
        }
        finally
        {
            _currentServerStateSemaphore.Release();
        }
    }

    public async Task<bool> GetIsServerOnlineAsync(TimeSpan timeout, bool checkUdp = true, CancellationToken cancellationToken = default)
    {
        if (checkUdp)
        {
            try
            {
                Task receiveMessageTask = _pingUdpClient.ReceiveMessageAsync(timeout, cancellationToken);
                await _pingUdpClient.SendPollingMessageAsync(_pingUdpCounter++, cancellationToken);
                await Task.Delay(200, cancellationToken);
                await receiveMessageTask;
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (InvalidDataException)
            {
            }
        }

        CancellationTokenSource timeoutCTS = new(timeout);
        CancellationTokenSource mergedCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCTS.Token);
        try
        {
            FactoryServerResponseContent<HealthCheckData> healthResponse = await _httpClient.HealthCheckAsync(null, mergedCTS.Token);
            HandleResponseContentErrors(healthResponse);
            return true;
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == mergedCTS.Token)
        {
            return false;
        }
        finally
        {
        }
    }

    public Task SetPlayerIdAsync(FactoryGamePlayerId playerId, CancellationToken cancellationToken = default)
    {
        return _httpClient.SetPlayerIdAsync(playerId, cancellationToken);
    }

    public Task ClearPlayerIdAsync(CancellationToken cancellationToken = default)
    {
        return _httpClient.ClearPlayerIdAsync(cancellationToken);
    }

    public async Task<bool> GetIsAuthenticationTokenValidAsync(CancellationToken cancellationToken = default)
    {
        if (_httpClient.AuthenticationToken is not null)
        {
            FactoryServerError? verifyError = await _httpClient.VerifyAuthenticationTokenAsync(cancellationToken);
            return verifyError is null;
        }
        return false;
    }

    public async Task SetAuthenticationTokenAsync(string authToken, CancellationToken cancellationToken = default)
    {
        await _httpClient.SetAuthenticationTokenAsync(authToken, cancellationToken);
    }

    public async Task AdministratorLoginAsync(ReadOnlyMemory<char> password, CancellationToken cancellationToken = default)
    {
        FactoryServerResponseContent<LoginData> loginResult = await _httpClient.PasswordLoginAsync(FactoryServerPrivilegeLevel.Administrator, password, cancellationToken);
        if (HandleResponseContentErrors(loginResult))
            return;

        await SetAuthenticationTokenAsync(loginResult.Data!.AuthenticationToken, cancellationToken);
    }

    public async Task ClientLoginAsync(ReadOnlyMemory<char>? password, CancellationToken cancellationToken = default)
    {
        Task<FactoryServerResponseContent<LoginData>> task = password is null
            ? _httpClient.PasswordlessLoginAsync(FactoryServerPrivilegeLevel.Client, cancellationToken)
            : _httpClient.PasswordLoginAsync(FactoryServerPrivilegeLevel.Client, password.Value, cancellationToken);

        FactoryServerResponseContent<LoginData> loginResult = await task;

        if (HandleResponseContentErrors(loginResult))
            return;

        await SetAuthenticationTokenAsync(loginResult.Data!.AuthenticationToken, cancellationToken);
    }

    public Task ClearLoginDataAsync(CancellationToken cancellationToken = default)
    {
        return _httpClient.ClearAuthenticationTokenAsync(cancellationToken);
    }

    public async Task ClaimServerAsync(string serverName, ReadOnlyMemory<char> adminPassword, CancellationToken cancellationToken = default)
    {
        //we try get a new InitialAdmin token regardless of the current token
        await _currentServerStateSemaphore.WaitAsync(cancellationToken);
        try
        {
            ClaimCheck claimCheck = await CheckIfServerIsClaimed(cancellationToken);
            _serverInfo.IsClaimed = claimCheck.IsClaimed;
            if (claimCheck.IsClaimed)
                throw new InvalidOperationException("The server is already claimed.");

            await _httpClient.SetAuthenticationTokenAsync(claimCheck.InitialAdminAuthToken!, cancellationToken);

            FactoryServerResponseContent<LoginData> claimResult = await _httpClient.ClaimServerAsync(serverName, adminPassword, cancellationToken);
            if (HandleResponseContentErrors(claimResult))
                return;

            _serverInfo.IsClaimed = true;
            await SetAuthenticationTokenAsync(claimResult.Data!.AuthenticationToken, cancellationToken);
        }
        finally
        {
            _currentServerStateSemaphore.Release();
        }
    }

    public async Task InitializeClientAsync(CancellationToken cancellationToken = default)
    {
        ClaimCheck claimCheck = await CheckIfServerIsClaimed(cancellationToken);
        _serverInfo.IsClaimed = claimCheck.IsClaimed;

        _ = ProcessChangedSubStatesQueue(cancellationToken);
        _ = _pollingUdpClient.StartPollingAsync(cancellationToken: cancellationToken);
    }

    private async Task<ClaimCheck> CheckIfServerIsClaimed(CancellationToken cancellationToken = default)
    {
        FactoryServerResponseContent<LoginData> initialAdminTryResponse = await _httpClient.PasswordlessLoginAsync(FactoryServerPrivilegeLevel.InitialAdmin, cancellationToken);
        if (HandleResponseContentErrors(initialAdminTryResponse))
        {
            return new(true, null, initialAdminTryResponse.Error); //If we can't, the server is claimed
        }
        return new(false, initialAdminTryResponse.Data!.AuthenticationToken, null);
    }

    private void UdpClient_ErrorOccurred(object? sender, Exception e)
    {
        FactoryServerErrorEventArgs args = new(e);
        ErrorOccurred?.Invoke(this, args);
    }

    private async Task ProcessChangedSubStatesQueue(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            FactoryServerState serverState = _serverInfo.ServerState;

            if (serverState != FactoryServerState.Loading
                && serverState != FactoryServerState.Offline
                && _statesToRequestQueue.TryDequeue(out FactoryServerSubState subState))
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
        try
        {
            if (_serverInfo.ServerState == FactoryServerState.Offline)
            {
                await HandleFirstTimeServerQueries(e);
            }
            else
            {
                if (e.ServerNetCL != _serverInfo.ChangeList) //improbable, but
                {
                    ErrorOccurred?.Invoke(this, new FactoryServerErrorEventArgs(new NotSupportedException("Server and client CL version differ.")));
                    return;
                }

                await UpdateCurrentStateAsync(e);

                IReadOnlyList<FactoryServerSubState> subStates = e.SubStates;
                IEnumerable<FactoryServerSubState> changedSubStates = _cachedSubStatesVersions.Count == 0
                    ? subStates
                    : subStates.Where(sS => !_cachedSubStatesVersions.ContainsKey(sS.SubStateId) || sS.SubStateVersion != _cachedSubStatesVersions[sS.SubStateId]);

                foreach (FactoryServerSubState subState in changedSubStates)
                    EnqueueSubStateQuery(subState, e.Cookie);
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, new FactoryServerErrorEventArgs(ex));
        }
    }

    private async Task UpdateCurrentStateAsync(FactoryServerStateUdpResponse response, CancellationToken cancellationToken = default)
    {
        FactoryServerResponseContent<HealthCheckData> healthResponse =
            await _httpClient.HealthCheckAsync(null, cancellationToken: cancellationToken);

        if (HandleResponseContentErrors(healthResponse))
            throw new InvalidOperationException("Something happened to http server.");

        await _currentServerStateSemaphore.WaitAsync(cancellationToken);
        try
        {
            _serverInfo.UpdateValue(response);
            _serverInfo.UpdateValue(healthResponse.Data!);

            ServerStateChanged?.Invoke(this, new FactoryServerStateChangedEventArgs(_serverInfo));
        }
        finally
        {
            _currentServerStateSemaphore.Release();
        }
    }

    private void EnqueueSubStateQuery(FactoryServerSubState subState, ulong cookie)
    {
        //if there is a pending request of the same type don't add new one, but we update the cookie
        //for now is useless, but can be used to improve state caching
        if (_subStateIdsQueriesUniquenessGuard.TryAdd(subState.SubStateId, cookie))
            _statesToRequestQueue.Enqueue(subState);
        else
            _subStateIdsQueriesUniquenessGuard[subState.SubStateId] = cookie;
    }

    private async Task HandleFirstTimeServerQueries(FactoryServerStateUdpResponse response, CancellationToken cancellationToken = default)
    {
        static async Task<T?> TryAwaitRequest<T>(Task<T> task) where T : class
        {
            try
            {
                return await task;
            }
            catch (Exception)
            {
                return null;
            }
        }

        var healthTask = _httpClient.HealthCheckAsync(null, cancellationToken);

        var queryStateTask = TryAwaitRequest(_httpClient.QueryServerStateAsync(cancellationToken));
        var optionsTask = TryAwaitRequest(_httpClient.GetServerOptionsAsync(cancellationToken));
        var advancedSettingsTask = TryAwaitRequest(_httpClient.GetAdvancedGameSettingsAsync(cancellationToken));
        var sessionsTask = TryAwaitRequest(_httpClient.EnumerateSessionsAsync(cancellationToken));

        FactoryServerResponseContent<HealthCheckData> healthResponse = await healthTask;

        await Task.WhenAll(queryStateTask, optionsTask, advancedSettingsTask, sessionsTask);

        var queryServerStateResponse = queryStateTask.Result;
        var optionsResponse = optionsTask.Result;
        var advancedSettingsResponse = advancedSettingsTask.Result;
        var sessionsResponse = sessionsTask.Result;

        await _currentServerStateSemaphore.WaitAsync(cancellationToken);
        try
        {
            _serverInfo.UpdateValue(response);

            if (HandleResponseContentErrors(healthResponse))
                throw new InvalidOperationException("Something happened to http server.");
            else
                _serverInfo.UpdateValue(healthResponse.Data!);

            if (queryServerStateResponse != null && !HandleResponseContentErrors(queryServerStateResponse))
                _serverInfo.UpdateValue(queryServerStateResponse.Data!);

            if (optionsResponse != null && !HandleResponseContentErrors(optionsResponse))
                _serverInfo.UpdateValue(optionsResponse.Data!);

            if (advancedSettingsResponse != null && !HandleResponseContentErrors(advancedSettingsResponse))
                _serverInfo.UpdateValue(advancedSettingsResponse.Data!);

            if (sessionsResponse != null && !HandleResponseContentErrors(sessionsResponse))
                _serverInfo.UpdateValue(sessionsResponse.Data!);

            ServerStateChanged?.Invoke(this, new FactoryServerStateChangedEventArgs(_serverInfo));
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
                await ProcessSubStateAsync(subState, httpClient => httpClient.QueryServerStateAsync(), cancellationToken);
                break;
            case FactoryServerSubStateId.ServerOptions:
                await ProcessSubStateAsync(subState, httpClient => httpClient.GetServerOptionsAsync(), cancellationToken);
                break;
            case FactoryServerSubStateId.AdvancedGameSettings:
                await ProcessSubStateAsync(subState, httpClient => httpClient.GetAdvancedGameSettingsAsync(), cancellationToken);
                break;
            case FactoryServerSubStateId.SaveCollection:
                await ProcessSubStateAsync(subState, httpClient => httpClient.EnumerateSessionsAsync(), cancellationToken);
                break;
            default:
                await ProcessCustomSubStateAsync(subState, cancellationToken);
                break;
        }
    }

    private async Task ProcessCustomSubStateAsync(FactoryServerSubState subState, CancellationToken cancellationToken)
    {
        await _currentServerStateSemaphore.WaitAsync(cancellationToken);
        try
        {
            bool gracefullyManaged = await HandleCustomSubStateAsync(_serverInfo, subState, _httpClient, cancellationToken);
            if (gracefullyManaged)
                UpdateSubStateCache(subState);
        }
        finally
        {
            _currentServerStateSemaphore.Release();
        }
    }

    protected virtual ValueTask<bool> HandleCustomSubStateAsync(FactoryServerInfo currentServerData, FactoryServerSubState subState, IFactoryServerHttpClient factoryHttpClient, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(true);
    }

    private async Task ProcessSubStateAsync<T>(FactoryServerSubState subState, Func<IFactoryServerHttpClient, Task<FactoryServerResponseContent<T>>> queryFunc, CancellationToken cancellationToken = default)
        where T : FactoryServerResponseContentData
    {
        await _currentServerStateSemaphore.WaitAsync(cancellationToken);
        try
        {
            FactoryServerResponseContent<T> responseContent = await queryFunc(_httpClient);

            if (HandleResponseContentErrors(responseContent))
                return;

            UpdateServerSubState(subState, responseContent.Data!);
            ServerStateChanged?.Invoke(this, new FactoryServerStateChangedEventArgs(_serverInfo));
        }
        finally
        {
            _currentServerStateSemaphore.Release();
        }
    }

    private void UpdateServerSubState<T>(FactoryServerSubState handledSubState, T result)
        where T : notnull
    {
        //if everything is ok we update the cached version
        if (result is QueryServerStateData stateData)
        {
            _serverInfo.UpdateValue(stateData);
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

    public void Dispose()
    {
        _pollingUdpClient.StopPollingAsync();
        _pollingUdpClient.Dispose();
        _pingUdpClient.StopPollingAsync();
        _pingUdpClient.Dispose();
    }
}
