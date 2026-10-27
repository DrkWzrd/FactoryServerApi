using System.IO;
using System.Text.Json;
using FactoryServerApi;
using FactoryServerApi.Http;
using FactoryServerApi.Http.Responses;
using FactoryServerApi.Udp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FactoryServerUi;

public sealed class MainForm : Form
{
    private const int MaxLogChars = 200_000;

    private readonly TrustedCertStore _certStore = new();
    private readonly Button[] _commandButtons;
    private readonly Dictionary<TabPage, Button[]> _tabButtons = new();

    private TextBox _address = null!;
    private Button _connect = null!;
    private Button _disconnect = null!;
    private ToolStripStatusLabel _status = null!;
    private ToolStripStatusLabel _connLabel = null!;
    private TextBox _log = null!;
    private Button _clearLog = null!;

    private Button _adminLogin = null!;
    private Button _claim = null!;
    private Button _setAdminPassword = null!;
    private Button _isOnline = null!;
    private Button _getState = null!;
    private Button _queryState = null!;
    private Button _getOptions = null!;
    private Button _getAdvSettings = null!;
    private Button _createGame = null!;
    private Button _loadGame = null!;
    private Button _saveGame = null!;
    private Button _enumerateSaves = null!;
    private Button _downloadSave = null!;
    private Button _uploadSave = null!;
    private Button _deleteSaveFile = null!;
    private Button _deleteSaveSession = null!;
    private Button _autoLoadSession = null!;
    private Button _rename = null!;
    private Button _setClientPassword = null!;
    private Button _runCommand = null!;
    private Button _applyOptions = null!;
    private Button _applyAdvSettings = null!;
    private Button _shutdown = null!;

    private TabPage _tabAuth = null!;
    private TabPage _tabState = null!;
    private TabPage _tabSaves = null!;
    private TabPage _tabAdmin = null!;

    private IFactoryServerClient? _client;
    private string? _initialAdminToken;
    private IServiceProvider? _services;
    private bool _busy;
    private string? _lastUdpState;

    public MainForm()
    {
        Text = "Factory Server UI";
        ClientSize = new Size(1080, 680);
        MinimumSize = new Size(820, 480);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);

        // --- Status bar (bottom) ---
        StatusStrip statusStrip = new() { SizingGrip = false };
        _status = new ToolStripStatusLabel
        {
            Text = "Not connected.",
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = false
        };
        _connLabel = new ToolStripStatusLabel
        {
            Text = "● Disconnected",
            ForeColor = Color.Firebrick,
            Font = new Font(Font, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleRight,
            AutoSize = false,
            Width = 150
        };
        statusStrip.Items.Add(_status);
        statusStrip.Items.Add(_connLabel);

        // --- Right pane: UDP controls on top, log below ---
        Panel right = new() { Dock = DockStyle.Fill, Padding = new Padding(12) };
        GroupBox udpGroup = new()
        {
            Text = "UDP polling",
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(4)
        };
        Button udpStart = MakeButton("Start polling", 116);
        Button udpStop = MakeButton("Stop polling", 116);
        _clearLog = MakeButton("Clear log", 116);
        udpStart.Click += OnUdpStart;
        udpStop.Click += OnUdpStop;
        _clearLog.Click += (_, _) => _log.Clear();
        FlowLayoutPanel udpRow = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(2)
        };
        udpRow.Controls.Add(udpStart);
        udpRow.Controls.Add(udpStop);
        udpRow.Controls.Add(_clearLog);
        udpGroup.Controls.Add(udpRow);
        _log = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            WordWrap = false,
            Font = new Font("Consolas", 9.5F),
            BackColor = SystemColors.Window
        };
        right.Controls.Add(_log);
        right.Controls.Add(udpGroup);

        // --- Left pane: connection group on top, command tabs below ---
        Panel left = new() { Dock = DockStyle.Left, Width = 400, Padding = new Padding(12, 12, 4, 12) };
        GroupBox connGroup = new()
        {
            Text = "Connection",
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(4)
        };
        Label addrLabel = new() { Text = "Server (host:port)", AutoSize = true, Dock = DockStyle.Top, Margin = new Padding(4, 2, 4, 2) };
        _address = new TextBox { Dock = DockStyle.Top, Height = 23, Text = "127.0.0.1:7777", Margin = new Padding(4, 2, 4, 4) };
        _connect = MakeButton("Connect", 100);
        _disconnect = MakeButton("Disconnect", 104);
        FlowLayoutPanel btnRow = new()
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(2)
        };
        btnRow.Controls.Add(_connect);
        btnRow.Controls.Add(_disconnect);
        // Add in reverse visual order: last added docks topmost.
        connGroup.Controls.Add(btnRow);
        connGroup.Controls.Add(_address);
        connGroup.Controls.Add(addrLabel);

        // Command tabs (one per section).
        TabControl tabs = new() { Dock = DockStyle.Fill };
        _tabAuth = MakeTab("Authentication",
            _adminLogin = MakeButton("Admin login"),
            _claim = MakeButton("Claim server"),
            _setAdminPassword = MakeButton("Set admin password"));
        _tabState = MakeTab("Server state",
            _isOnline = MakeButton("Is server online"),
            _getState = MakeButton("Get server state"),
            _queryState = MakeButton("Query state"),
            _getOptions = MakeButton("Get server options"),
            _getAdvSettings = MakeButton("Get advanced settings"));
        _tabSaves = MakeTab("Game & saves",
            _createGame = MakeButton("Create new game"),
            _loadGame = MakeButton("Load game"),
            _saveGame = MakeButton("Save game"),
            _enumerateSaves = MakeButton("Enumerate saves"),
            _downloadSave = MakeButton("Download save"),
            _uploadSave = MakeButton("Upload save"),
            _deleteSaveFile = MakeButton("Delete save file"),
            _deleteSaveSession = MakeButton("Delete save session"),
            _autoLoadSession = MakeButton("Set auto-load session"));
        _tabAdmin = MakeTab("Admin",
            _rename = MakeButton("Rename server"),
            _setClientPassword = MakeButton("Set client password"),
            _runCommand = MakeButton("Run command"),
            _applyOptions = MakeButton("Apply server options"),
            _applyAdvSettings = MakeButton("Apply advanced settings"),
            _shutdown = MakeButton("Shutdown server"));
        tabs.TabPages.AddRange(new[] { _tabAuth, _tabState, _tabSaves, _tabAdmin });

        // Wire up the command handlers.
        _connect.Click += OnConnect;
        _disconnect.Click += OnDisconnect;
        _adminLogin.Click += OnAdminLogin;
        _claim.Click += OnClaim;
        _setAdminPassword.Click += OnSetAdminPassword;
        _isOnline.Click += OnIsOnline;
        _getState.Click += OnGetState;
        _queryState.Click += OnQueryState;
        _getOptions.Click += OnGetOptions;
        _getAdvSettings.Click += OnGetAdvSettings;
        _createGame.Click += OnCreateGame;
        _loadGame.Click += OnLoadGame;
        _saveGame.Click += OnSaveGame;
        _enumerateSaves.Click += OnEnumerateSaves;
        _downloadSave.Click += OnDownloadSave;
        _uploadSave.Click += OnUploadSave;
        _deleteSaveFile.Click += OnDeleteSaveFile;
        _deleteSaveSession.Click += OnDeleteSaveSession;
        _autoLoadSession.Click += OnSetAutoLoadSession;
        _rename.Click += OnRename;
        _setClientPassword.Click += OnSetClientPassword;
        _runCommand.Click += OnRunCommand;
        _applyOptions.Click += OnApplyOptions;
        _applyAdvSettings.Click += OnApplyAdvSettings;
        _shutdown.Click += OnShutdown;
        FormClosing += OnFormClosing;

        // Every command button toggles with the connection state and busy flag.
        _commandButtons =
        [
            _adminLogin, _claim, _setAdminPassword,
            _isOnline, _getState, _queryState, _getOptions, _getAdvSettings,
            _createGame, _loadGame, _saveGame, _enumerateSaves, _downloadSave, _uploadSave, _deleteSaveFile, _deleteSaveSession, _autoLoadSession,
            _rename, _setClientPassword, _runCommand, _applyOptions, _applyAdvSettings, _shutdown,
            udpStart, udpStop
        ];

        left.Controls.Add(tabs);
        left.Controls.Add(connGroup);

        // Fill pane first, then the left-docked pane, then the bottom status strip.
        Controls.Add(right);
        Controls.Add(left);
        Controls.Add(statusStrip);

        SetConnected(false);
    }

    private static Button MakeButton(string text, int width = 150)
        => new() { Text = text, Width = width, Height = 28, Margin = new Padding(4), AutoSize = false, TextAlign = ContentAlignment.MiddleCenter };

    /// <summary>
    /// Builds a tab page whose body is a wrapping flow of command buttons, and
    /// registers the buttons so the tab can be hidden when none are visible.
    /// </summary>
    private TabPage MakeTab(string title, params Button[] buttons)
    {
        TabPage page = new(title) { Padding = new Padding(6) };
        FlowLayoutPanel flow = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = true,
            AutoScroll = true,
            Padding = new Padding(4)
        };
        foreach (Button b in buttons)
            flow.Controls.Add(b);
        page.Controls.Add(flow);
        _tabButtons[page] = buttons;
        return page;
    }

    // --- Connection lifecycle ---

    private async void OnConnect(object? sender, EventArgs e)
    {
        string address = _address.Text.Trim();
        (string host, int port) = ParseAddress(address);
        _lastCertLine = null;

        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        CertTrustValidator certValidator = new(this, _certStore, $"{host}:{port}");
        certValidator.CertificateChecked += OnCertificateChecked;
        builder.Services.AddSingleton<IServerCertificateValidationStrategy>(certValidator);
        builder.AddFactoryServerApiServices();
        // Registered after AddFactoryServerApiServices on purpose: its
        // sslConfiguration binder would otherwise override this with the
        // settings-file value (NoValidation).
        builder.Services.Configure<SslOptions>(o => o.ServerCertificateValidationStrategy = SslValidationStrategy.Custom);
        IServiceProvider app = builder.Services.BuildServiceProvider();

        IFactoryServerClient? session = null;
        try
        {
            IFactoryServerClientFactory factory = app.GetRequiredService<IFactoryServerClientFactory>();
            session = await factory.BuildFactoryServerClientAsync(host, port);
            session.ServerStateReceived += OnServerStateReceived;
            session.ErrorOccurred += OnClientError;

            // First action: check the claim state. An unclaimed server yields an
            // InitialAdmin token to claim with (prompting for name + password).
            FactoryServerClaimCheck check = await session.CheckClaimStateAsync();
            if (check.Unclaimed)
            {
                _initialAdminToken = check.InitialAdminToken;
                (string serverName, ReadOnlyMemory<char> adminPassword) = await ClaimServerPrompt();
                if (!await session.ClaimServerAsync(serverName, adminPassword, check.InitialAdminToken!))
                    throw new InvalidOperationException("Failed to claim the server.");
                _initialAdminToken = null; // the claim invalidated the InitialAdmin token
            }
            if (session.CurrentPrivilegeLevel is not (FactoryServerPrivilegeLevel.Administrator or FactoryServerPrivilegeLevel.APIToken))
            {
                ReadOnlyMemory<char> adminPassword = await AdminPasswordPrompt();
                if (!await session.AdministratorLoginAsync(adminPassword))
                    throw new InvalidOperationException("Failed to log in as Administrator.");
            }

            _client = session;
            _services = app;
            session = null;
            app = null!;
            SetConnected(true);
            SetStatus($"Connected to {host}:{port} — privilege: {_client.CurrentPrivilegeLevel.ToString()}, claimed: {_client.IsClaimed}");
        }
        catch (OperationCanceledException)
        {
            SetStatus("Connection cancelled.");
            CleanupClient(session, app);
        }
        catch (Exception ex)
        {
            SetStatus($"Connection failed: {ex.Message}");
            CleanupClient(session, app);
        }
    }

    /// <summary>
    /// Teardown runs off the UI thread: stopping the poll loop and closing a UDP
    /// socket that has an in-flight receive can block for seconds.
    /// </summary>
    private void CleanupClient(IFactoryServerClient? session, IServiceProvider? services)
    {
        if (session is null && services is null)
            return;

        _ = Task.Run(async () =>
        {
            if (session is not null)
            {
                session.ServerStateReceived -= OnServerStateReceived;
                session.ErrorOccurred -= OnClientError;
                await session.StopPollingAsync();
                session.Dispose();
            }
            (services as IDisposable)?.Dispose();
        });
    }

    private async void OnDisconnect(object? sender, EventArgs e)
    {
        IFactoryServerClient? session = _client;
        IServiceProvider? services = _services;
        if (session is null)
            return;

        // Detach from the UI's point of view immediately; the actual teardown
        // (stopping the poll loop, closing the socket) can block and must not
        // run on the UI thread.
        _client = null;
        _initialAdminToken = null;
        _services = null;
        SetConnected(false);
        SetBusy(true);
        SetStatus("Disconnecting…");

        try
        {
            await Task.Run(async () =>
            {
                session.ServerStateReceived -= OnServerStateReceived;
                session.ErrorOccurred -= OnClientError;
                await session.StopPollingAsync();
                session.Dispose();
                (services as IDisposable)?.Dispose();
            });
            SetStatus("Disconnected.");
        }
        catch (Exception ex)
        {
            SetStatus($"Disconnect failed: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        IFactoryServerClient? session = _client;
        IServiceProvider? services = _services;
        _client = null;
        _initialAdminToken = null;
        _services = null;

        // Best effort: the app is exiting, so don't wait for the teardown.
        if (session is not null)
        {
            session.ServerStateReceived -= OnServerStateReceived;
            session.ErrorOccurred -= OnClientError;
            _ = Task.Run(() =>
            {
                session.StopPollingAsync().GetAwaiter().GetResult();
                session.Dispose();
                (services as IDisposable)?.Dispose();
            });
        }
    }

    // --- Credential prompts (invoked on the UI thread) ---

    private Task<(string ServerName, ReadOnlyMemory<char> AdminPassword)> ClaimServerPrompt()
    {
        string? name = Prompts.Input(this, "Claim server", "Server name:", "My Factory Server");
        if (name is null || name.Length == 0)
            return Task.FromCanceled<(string ServerName, ReadOnlyMemory<char> AdminPassword)>(CancellationToken.None);

        char[]? password = Prompts.Password(this, "Claim server", "Choose an admin password:");
        if (password is null)
            return Task.FromCanceled<(string ServerName, ReadOnlyMemory<char> AdminPassword)>(CancellationToken.None);

        char[]? confirm = Prompts.Password(this, "Claim server", "Confirm admin password:");
        if (confirm is null || !password.SequenceEqual(confirm))
        {
            MessageBox.Show(this, "Passwords do not match.", "Claim server", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return Task.FromCanceled<(string ServerName, ReadOnlyMemory<char> AdminPassword)>(CancellationToken.None);
        }

        return Task.FromResult(((string ServerName, ReadOnlyMemory<char> AdminPassword))(name, password.AsMemory()));
    }

    private Task<ReadOnlyMemory<char>> AdminPasswordPrompt()
    {
        char[]? password = Prompts.Password(this, "Admin login", "Admin password:");
        if (password is null)
            return Task.FromCanceled<ReadOnlyMemory<char>>(CancellationToken.None);
        return Task.FromResult<ReadOnlyMemory<char>>(password.AsMemory());
    }

    // --- Log / UDP state ---

    private string? _lastCertLine;

    /// <summary>Logs the certificate hash check result (runs on the HTTP thread; deduped per connection).</summary>
    private void OnCertificateChecked(CertTrustValidator.CertCheckResult r)
    {
        string outcome = !r.Allowed ? "rejected" : r.ChainValid ? "chain valid" : "trusted (previously accepted)";
        int cn = r.Subject.IndexOf("CN=", StringComparison.Ordinal);
        string subject = cn >= 0 ? r.Subject[(cn + 3)..] : r.Subject;
        int comma = subject.IndexOf(',');
        if (comma >= 0) subject = subject[..comma];
        string line = $"Certificate check: {subject} — SHA256:{r.ServerLogFingerprintB64} — {outcome}";
        if (line == _lastCertLine)
            return;
        _lastCertLine = line;
        if (InvokeRequired)
            BeginInvoke(() => Log(line));
        else
            Log(line);
    }

    private async void OnServerStateReceived(object? sender, FactoryServerStateUdpResponse state)
    {
        // Only log when something actually changed, so polling doesn't spam the log.
        string line = FormatUdpState(state);
        if (line == _lastUdpState)
            return;
        _lastUdpState = line;
        string full = await FormatUdpStateFullAsync(state);
        if (InvokeRequired)
            BeginInvoke(() => Log(full));
        else
            Log(full);
    }

    /// <summary>Formats the UDP state as JSON, with each reported sub-state's deserialized payload fetched from the server.</summary>
    private async Task<string> FormatUdpStateFullAsync(FactoryServerStateUdpResponse state)
    {
        Dictionary<string, object?>? subStates = null;
        try
        {
            subStates = await FetchSubStatesAsync(state);
        }
        catch (Exception ex)
        {
            Log($"Sub-state fetch failed: {ex.Message}");
        }
        return FormatUdpState(state, subStates);
    }

    /// <summary>
    /// Fetches the deserialized payload for each sub-state reported in the UDP state.
    /// Returns null when the state has no sub-states (or no session is available).
    /// </summary>
    private async Task<Dictionary<string, object?>?> FetchSubStatesAsync(FactoryServerStateUdpResponse state)
    {
        IFactoryServerClient? session = _client;
        if (session is null || state.SubStates.Count == 0)
            return null;
        Dictionary<string, object?> subStates = new();
        foreach (FactoryServerSubState sub in state.SubStates)
        {
            object? data = sub.SubStateId switch
            {
                FactoryServerSubStateId.ServerGameState => (await session.QueryServerStateAsync()).Data,
                FactoryServerSubStateId.ServerOptions => (await session.GetServerOptionsAsync()).Data,
                FactoryServerSubStateId.AdvancedGameSettings => (await session.GetAdvancedGameSettingsAsync()).Data,
                FactoryServerSubStateId.SaveCollection => (await session.EnumerateSessionsAsync()).Data,
                _ => null // Custom1..Custom4 have no HTTPS API mapping
            };
            subStates[SubStateKey(sub.SubStateId)] = data;
        }
        return subStates;
    }

    private static string SubStateKey(FactoryServerSubStateId id)
    {
        string name = id.ToString();
        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    private void OnClientError(object? sender, Exception ex)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] ERROR {ex.Message}";
        if (InvokeRequired)
            BeginInvoke(() => AppendLog(line));
        else
            AppendLog(line);
    }

    private void AppendLog(string line)
    {
        _log.AppendText(line + "\r\n");
        if (_log.TextLength > MaxLogChars)
            _log.Text = _log.Text[^100_000..];
    }

    /// <summary>Appends a timestamped line to the log (UI thread).</summary>
    private void Log(string message)
        => AppendLog($"[{DateTime.Now:HH:mm:ss}] {message}");

    private static readonly JsonSerializerOptions UdpJsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// Formats a UDP state response as clean, indented JSON (no timestamp; the log adds it).
    /// <paramref name="subStates"/> holds the deserialized payload per sub-state; when null, the
    /// raw sub-state id list is shown instead. Sub-state versions are omitted: they tick up on
    /// almost every poll, which would defeat the change-detection and spam the log.
    /// </summary>
    private static string FormatUdpState(FactoryServerStateUdpResponse s, Dictionary<string, object?>? subStates = null)
    {
        Dictionary<string, object?> state = new()
        {
            ["cookie"] = s.Cookie,
            ["serverState"] = s.ServerState,
            ["serverNetCL"] = s.ServerNetCL,
            ["serverFlags"] = s.ServerFlags,
            ["serverName"] = s.ServerName,
            ["subStates"] = subStates ?? (object?)s.SubStates.Select(x => x.SubStateId).ToArray(),
            ["receivedUtc"] = s.ReceivedUtc
        };
        return JsonSerializer.Serialize(state, UdpJsonOptions);
    }

    private static string FormatGameState(ServerGameState g) =>
        $"session=\"{g.ActiveSessionName}\" players={g.NumConnectedPlayers}/{g.PlayerLimit} tier={g.TechTier} " +
        $"phase={g.GamePhase} running={g.IsGameRunning} paused={g.IsGamePaused} avgTick={g.AverageTickRate:0.##}s autoLoad=\"{g.AutoLoadSessionName}\"";

    private static string FormatDict(IReadOnlyDictionary<string, string> d)
        => d.Count == 0 ? "(none)" : string.Join(", ", d.Select(kv => $"{kv.Key}={kv.Value}"));

    // --- Polling ---

    private void OnUdpStart(object? sender, EventArgs e)
        => _ = Guard(() => _client!.StartPollingAsync());

    private void OnUdpStop(object? sender, EventArgs e)
        => _ = Guard(() => _client!.StopPollingAsync());

    // --- Authentication ---

    private async void OnAdminLogin(object? sender, EventArgs e)
    {
        if (_client is null)
            return;

        char[]? password = Prompts.Password(this, "Admin login", "Admin password:");
        if (password is null)
            return;

        if (await Guard(() => _client.AdministratorLoginAsync(password.AsMemory())))
        {
            SetStatus($"Logged in — privilege: {_client.CurrentPrivilegeLevel.ToString()}");
            UpdateVisibility();
        }
        else
        {
            MessageBox.Show(this, "Login failed (wrong password?).", "Admin login", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async void OnClaim(object? sender, EventArgs e)
    {
        if (_client is null)
            return;

        // Re-acquire the InitialAdmin token if we don't have one (e.g. after a failed claim).
        if (_initialAdminToken is null)
        {
            FactoryServerClaimCheck check = await _client.CheckClaimStateAsync();
            if (!check.Unclaimed)
                return; // the server is claimed
            _initialAdminToken = check.InitialAdminToken;
        }

        string? name = Prompts.Input(this, "Claim server", "Server name:", "My Factory Server");
        if (name is null || name.Length == 0)
            return;

        char[]? password = Prompts.Password(this, "Claim server", "Admin password:");
        if (password is null)
            return;

        if (await Guard(() => _client!.ClaimServerAsync(name, password.AsMemory(), _initialAdminToken!)))
        {
            _initialAdminToken = null; // the claim invalidated the InitialAdmin token
            SetStatus($"Claimed — privilege: {_client.CurrentPrivilegeLevel.ToString()}, claimed: {_client.IsClaimed}");
            UpdateVisibility();
        }
        else
        {
            MessageBox.Show(this, "Claim failed (server already claimed?).", "Claim server", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async void OnSetAdminPassword(object? sender, EventArgs e)
    {
        if (_client is null)
            return;

        char[]? password = Prompts.Password(this, "Set admin password", "New admin password:");
        if (password is null)
            return;

        string? token = Prompts.Input(this, "Set admin password", "New authentication token issued by the server:");
        if (token is null || token.Length == 0)
            return;

        if (await Guard(() => _client.SetAdminPasswordAsync(password.AsMemory(), token)))
        {
            SetStatus("Admin password updated.");
            UpdateVisibility();
        }
        else
        {
            MessageBox.Show(this, "Failed to update the admin password.", "Set admin password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // --- Server state ---

    private async void OnIsOnline(object? sender, EventArgs e)
    {
        if (_client is null)
            return;

        SetBusy(true);
        try
        {
            bool online = await _client.GetIsServerOnlineAsync(TimeSpan.FromSeconds(5));
            Log($"Server online: {online}");
        }
        catch (Exception ex)
        {
            Log($"Is online failed: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnGetState(object? sender, EventArgs e)
    {
        if (_client is null)
            return;

        SetBusy(true);
        try
        {
            FactoryServerStateUdpResponse? state = await _client.GetServerStateAsync();
            if (state is null)
                Log("No UDP state received.");
            else
            {
                _lastUdpState = FormatUdpState(state);
                Log(await FormatUdpStateFullAsync(state));
            }
        }
        catch (Exception ex)
        {
            Log($"Get state failed: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnQueryState(object? sender, EventArgs e)
    {
        if (_client is null)
            return;

        SetBusy(true);
        try
        {
            FactoryServerResponseContent<QueryServerStateData> result = await _client.QueryServerStateAsync();
            if (result.Data is not null)
                Log("Query state: " + FormatGameState(result.Data.ServerGameState));
            else
                Log($"Query state FAILED: {result.Error?.ErrorCode} {result.Error?.ErrorMessage}");
        }
        catch (Exception ex)
        {
            Log($"Query state failed: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnGetOptions(object? sender, EventArgs e)
    {
        if (_client is null)
            return;

        SetBusy(true);
        try
        {
            FactoryServerResponseContent<GetServerOptionsData> result = await _client.GetServerOptionsAsync();
            if (result.Data is not null)
                Log("Server options: " + FormatDict(result.Data.ServerOptions));
            else
                Log($"Get options FAILED: {result.Error?.ErrorCode} {result.Error?.ErrorMessage}");
        }
        catch (Exception ex)
        {
            Log($"Get options failed: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnGetAdvSettings(object? sender, EventArgs e)
    {
        if (_client is null)
            return;

        SetBusy(true);
        try
        {
            FactoryServerResponseContent<GetAdvancedGameSettingsData> result = await _client.GetAdvancedGameSettingsAsync();
            if (result.Data is not null)
                Log($"Advanced settings (creative={result.Data.CreativeModeEnabled}): " + FormatDict(result.Data.AdvancedGameSettings));
            else
                Log($"Get advanced settings FAILED: {result.Error?.ErrorCode} {result.Error?.ErrorMessage}");
        }
        catch (Exception ex)
        {
            Log($"Get advanced settings failed: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    // --- Game & saves ---

    private async void OnCreateGame(object? sender, EventArgs e)
    {
        if (_client is null)
            return;

        string? name = Prompts.Input(this, "Create new game", "Session name:");
        if (name is null || name.Length == 0)
            return;

        ServerNewGameData game = new(name) { SkipOnboarding = true };
        if (await Guard(() => _client.CreateNewGameAsync(game)))
            SetStatus($"New game requested: {name}");
    }

    private async void OnLoadGame(object? sender, EventArgs e)
    {
        if (_client is null)
            return;

        string? name = Prompts.Input(this, "Load game", "Save name:");
        if (name is null || name.Length == 0)
            return;

        DialogResult adv = MessageBox.Show(this, "Enable advanced game settings?", "Load game", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        if (adv == DialogResult.Cancel)
            return;

        if (await Guard(() => _client.LoadGameAsync(name, enableAdvancedGameSettings: adv == DialogResult.Yes)))
            SetStatus($"Load requested: {name}");
    }

    private async void OnSaveGame(object? sender, EventArgs e)
    {
        if (_client is null)
            return;

        string? name = Prompts.Input(this, "Save game", "Save name:");
        if (name is null || name.Length == 0)
            return;

        if (await Guard(() => _client.SaveGameAsync(name)))
            SetStatus($"Save requested: {name}");
    }

    private async void OnEnumerateSaves(object? sender, EventArgs e)
    {
        if (_client is null)
            return;

        SetBusy(true);
        try
        {
            FactoryServerResponseContent<EnumerateSessionsData> result = await _client.EnumerateSessionsAsync();
            if (result.Data is not null)
            {
                Log($"Saves ({result.Data.Sessions.Count} session(s)):");
                foreach (FactorySaveSession session in result.Data.Sessions)
                    foreach (FactorySaveFile save in session.SaveHeaders)
                        Log($"  [{session.SessionName}] {save.SaveName} — {save.MapName}, {save.SaveDateTime}");
                if (result.Data.Sessions.Count == 0)
                    Log("  (none)");
            }
            else
            {
                Log($"Enumerate FAILED: {result.Error?.ErrorCode} {result.Error?.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Log($"Enumerate failed: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>
    /// Asks the server for its current saves. Returns null (having reported the
    /// failure to the user) when the enumeration fails, so callers can bail out.
    /// </summary>
    private async Task<EnumerateSessionsData?> EnumerateAsync()
    {
        SetBusy(true);
        try
        {
            FactoryServerResponseContent<EnumerateSessionsData> result = await _client!.EnumerateSessionsAsync();
            if (result.Data is null)
            {
                MessageBox.Show(this, $"{result.Error?.ErrorCode}: {result.Error?.ErrorMessage}", "Enumerate saves", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
            return result.Data;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Enumerate saves", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return null;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private static string FormatSave(FactorySaveFile save)
        => $"[{save.SessionName}] {save.SaveName}  —  {save.MapName}, {save.SaveDateTime}";

    private async void OnDownloadSave(object? sender, EventArgs e)
    {
        if (_client is null)
            return;

        // First ask the server what saves exist, then let the user pick one.
        EnumerateSessionsData? data = await EnumerateAsync();
        if (data is null)
            return;

        List<FactorySaveFile> saves = data.Sessions.SelectMany(s => s.SaveHeaders).ToList();
        using ListPickerForm picker = new("Select a save to download", "Download", saves.Select(FormatSave).ToList());
        if (picker.ShowDialog(this) != DialogResult.OK)
            return;

        string name = saves[picker.SelectedIndex].SaveName;

        SetBusy(true);
        try
        {
            FactoryServerResponseContent<DownloadSaveGameData> result = await _client.DownloadSaveGameAsync(name);
            if (result.Data is null)
            {
                MessageBox.Show(this, $"{result.Error?.ErrorCode}: {result.Error?.ErrorMessage}", "Download save", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using SaveFileDialog dialog = new()
            {
                Filter = "Factory save (*.fsm)|*.fsm|All files (*.*)|*.*",
                FileName = name + ".fsm"
            };
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            await using Stream stream = result.Data.SaveFileStream;
            await stream.CopyToAsync(File.OpenWrite(dialog.FileName));
            SetStatus($"Downloaded \"{name}\" to {dialog.FileName}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Download save", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnUploadSave(object? sender, EventArgs e)
    {
        if (_client is null)
            return;

        using OpenFileDialog dialog = new()
        {
            Filter = "Factory save (*.fsm)|*.fsm|All files (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        string? name = Prompts.Input(this, "Upload save", "Save name:", Path.GetFileNameWithoutExtension(dialog.FileName));
        if (name is null || name.Length == 0)
            return;

        SetBusy(true);
        try
        {
            using Stream stream = File.OpenRead(dialog.FileName);
            FactoryServerError? error = await _client.UploadSaveGameAsync(name, loadSaveGame: false, enableAdvancedGameSettings: false, stream);
            if (error is not null)
                MessageBox.Show(this, $"{error.ErrorCode}: {error.ErrorMessage}", "Upload save", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
                SetStatus($"Uploaded \"{name}\"");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Upload save", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnDeleteSaveFile(object? sender, EventArgs e)
    {
        if (_client is null)
            return;

        EnumerateSessionsData? data = await EnumerateAsync();
        if (data is null)
            return;

        List<FactorySaveFile> saves = data.Sessions.SelectMany(s => s.SaveHeaders).ToList();
        using ListPickerForm picker = new("Select a save file to delete", "Delete", saves.Select(FormatSave).ToList());
        if (picker.ShowDialog(this) != DialogResult.OK)
            return;

        string name = saves[picker.SelectedIndex].SaveName;
        if (MessageBox.Show(this, $"Delete save file \"{name}\"?", "Delete save file", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        if (await Guard(() => _client.DeleteSaveFileAsync(name)))
            SetStatus($"Deleted save file: {name}");
    }

    private async void OnDeleteSaveSession(object? sender, EventArgs e)
    {
        if (_client is null)
            return;

        EnumerateSessionsData? data = await EnumerateAsync();
        if (data is null)
            return;

        List<string> sessions = data.Sessions.Select(s => s.SessionName).ToList();
        using ListPickerForm picker = new("Select a save session to delete", "Delete", sessions);
        if (picker.ShowDialog(this) != DialogResult.OK)
            return;

        string name = sessions[picker.SelectedIndex];
        if (MessageBox.Show(this, $"Delete save session \"{name}\"?", "Delete save session", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        if (await Guard(() => _client.DeleteSaveSessionAsync(name)))
            SetStatus($"Deleted save session: {name}");
    }

    private async void OnSetAutoLoadSession(object? sender, EventArgs e)
    {
        if (_client is null)
            return;

        EnumerateSessionsData? data = await EnumerateAsync();
        if (data is null)
            return;

        List<string> sessions = data.Sessions.Select(s => s.SessionName).ToList();
        using ListPickerForm picker = new("Select the auto-load session", "Set", sessions);
        if (picker.ShowDialog(this) != DialogResult.OK)
            return;

        string name = sessions[picker.SelectedIndex];
        if (await Guard(() => _client.SetAutoLoadSessionNameAsync(name)))
            SetStatus($"Auto-load session: {name}");
    }

    // --- Admin ---

    private async void OnRename(object? sender, EventArgs e)
    {
        if (_client is null)
            return;

        string? name = Prompts.Input(this, "Rename server", "New server name:");
        if (name is null || name.Length == 0)
            return;

        if (await Guard(() => _client.RenameServerAsync(name)))
            SetStatus($"Renamed to: {name}");
    }

    private async void OnSetClientPassword(object? sender, EventArgs e)
    {
        if (_client is null)
            return;

        char[]? password = Prompts.Password(this, "Set client password", "Client password (blank to clear):");
        if (password is null)
            return;

        ReadOnlyMemory<char>? pwd = password.Length == 0 ? null : password.AsMemory();
        if (await Guard(() => _client.SetClientPasswordAsync(pwd)))
            SetStatus($"Client password {(pwd is null ? "cleared" : "set")}");
    }

    private async void OnRunCommand(object? sender, EventArgs e)
    {
        if (_client is null)
            return;

        string? command = Prompts.Input(this, "Run command", "Server console command:");
        if (command is null || command.Length == 0)
            return;

        SetBusy(true);
        try
        {
            FactoryServerResponseContent<RunCommandData> result = await _client.RunCommandAsync(command);
            if (result.Data is not null)
                Log($"> {command}\n{result.Data.CommandResult}");
            else
                Log($"Run command FAILED: {result.Error?.ErrorCode} {result.Error?.ErrorMessage}");
        }
        catch (Exception ex)
        {
            Log($"Run command failed: {ex.Message}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void OnApplyOptions(object? sender, EventArgs e)
    {
        if (_client is null)
            return;

        Dictionary<string, string>? kv = Prompts.KeyValue(this, "Apply server options", "One option per line as key=value:");
        if (kv is null || kv.Count == 0)
            return;

        if (await Guard(() => _client.ApplyServerOptionsAsync(kv)))
            SetStatus($"Applied {kv.Count} server option(s).");
    }

    private async void OnApplyAdvSettings(object? sender, EventArgs e)
    {
        if (_client is null)
            return;

        Dictionary<string, string>? kv = Prompts.KeyValue(this, "Apply advanced settings", "One setting per line as key=value:");
        if (kv is null || kv.Count == 0)
            return;

        if (await Guard(() => _client.ApplyAdvancedGameSettingsAsync(kv)))
            SetStatus($"Applied {kv.Count} advanced setting(s).");
    }

    private async void OnShutdown(object? sender, EventArgs e)
    {
        if (_client is null)
            return;

        if (MessageBox.Show(this, "Shut down the server?", "Shutdown", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        // The server is going down (or has already failed to answer): once the
        // shutdown request is answered — success or first error — disconnect.
        bool ok = await Guard(() => _client.ShutdownAsync());
        SetStatus(ok ? "Shutdown requested — disconnecting." : "Shutdown failed — disconnecting.");
        OnDisconnect(this, EventArgs.Empty);
    }

    // --- Helpers ---

    private async Task<bool> Guard(Func<Task> action)
    {
        SetBusy(true);
        try
        {
            await action();
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task<bool> Guard(Func<Task<FactoryServerError?>> action)
    {
        SetBusy(true);
        try
        {
            FactoryServerError? error = await action();
            if (error is not null)
            {
                MessageBox.Show(this, $"{error.ErrorCode}: {error.ErrorMessage}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetConnected(bool connected)
    {
        _lastUdpState = null;
        _connect.Enabled = !connected;
        _address.ReadOnly = connected;
        _disconnect.Enabled = connected;
        _connLabel.Text = connected ? "● Connected" : "● Disconnected";
        _connLabel.ForeColor = connected ? Color.ForestGreen : Color.Firebrick;
        UpdateVisibility();
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        UpdateVisibility();
    }

    /// <summary>
    /// Shows only the commands that make sense for the current login state:
    /// admin commands for admins, login when logged out, claim only on
    /// unclaimed servers.
    /// </summary>
    private void UpdateVisibility()
    {
        IFactoryServerClient? session = _client;
        bool connected = session is not null;
        FactoryServerPrivilegeLevel priv = session?.CurrentPrivilegeLevel ?? FactoryServerPrivilegeLevel.NotAuthenticated;
        bool admin = priv is FactoryServerPrivilegeLevel.Administrator or FactoryServerPrivilegeLevel.APIToken;

        _adminLogin.Visible = connected && !admin;
        _claim.Visible = connected && _initialAdminToken is not null;
        _setAdminPassword.Visible = admin;

        _isOnline.Visible = connected;
        _getState.Visible = connected;
        _queryState.Visible = connected;
        _getOptions.Visible = connected;
        _getAdvSettings.Visible = connected;

        _createGame.Visible = admin;
        _loadGame.Visible = admin;
        _saveGame.Visible = admin;
        _uploadSave.Visible = admin;
        _deleteSaveFile.Visible = admin;
        _deleteSaveSession.Visible = admin;
        _autoLoadSession.Visible = admin;
        _enumerateSaves.Visible = connected;
        _downloadSave.Visible = connected;

        _rename.Visible = admin;
        _setClientPassword.Visible = admin;
        _runCommand.Visible = admin;
        _applyOptions.Visible = admin;
        _applyAdvSettings.Visible = admin;
        _shutdown.Visible = admin;

        // A tab is shown only while at least one of its buttons is visible.
        _tabAuth.Visible = AnyVisible(_tabAuth);
        _tabState.Visible = AnyVisible(_tabState);
        _tabSaves.Visible = AnyVisible(_tabSaves);
        _tabAdmin.Visible = AnyVisible(_tabAdmin);

        foreach (Button b in _commandButtons)
            b.Enabled = b.Visible && connected && !_busy;
    }

    private bool AnyVisible(TabPage page) => _tabButtons[page].Any(b => b.Visible);

    private void SetStatus(string text) => _status.Text = text;

    private static (string host, int port) ParseAddress(string address)
    {
        string[] parts = address.Split(':', 2);
        string host = parts[0].Trim();
        int port = parts.Length == 2 && int.TryParse(parts[1], out int p) ? p : 7777;
        if (host.Length == 0)
            throw new ArgumentException("Enter a server address (host:port).");
        return (host, port);
    }
}

/// <summary>
/// Generic single-selection list dialog. Shows a row of display strings and
/// returns the index of the chosen one (or -1 when cancelled).
/// </summary>
internal sealed class ListPickerForm : Form
{
    private readonly ListBox _list = new() { Dock = DockStyle.Fill, Font = new Font("Consolas", 9F) };

    public int SelectedIndex => _list.SelectedIndex;

    public ListPickerForm(string title, string acceptText, IReadOnlyList<string> items)
    {
        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(520, 380);

        FlowLayoutPanel buttons = new()
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(12)
        };
        Button ok = new() { Text = acceptText, Width = 110, Height = 27, DialogResult = DialogResult.OK };
        Button cancel = new() { Text = "Cancel", Width = 110, Height = 27, DialogResult = DialogResult.Cancel };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);

        foreach (string item in items)
            _list.Items.Add(item);

        _list.SelectedIndex = items.Count > 0 ? 0 : -1;
        ok.Enabled = items.Count > 0;

        Controls.Add(_list);
        Controls.Add(buttons);
        AcceptButton = ok;
        CancelButton = cancel;
    }
}
