using FactoryServerApiNew;
using FactoryServerApiNew.Http.Responses;
using FactoryServerApiNew.Udp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.Time;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

// ============================================================================
//  FactoryServerApiNew — TUI built on Terminal.Gui
//  Left pane : live UDP heartbeat (state, name, polls, history, age)
//  Right pane: interactive menu tree exposing the whole workflow
//
//  Usage: dotnet run --project tests/FactoryServerApi.Tests [host] [port]
// ============================================================================

string host = args.Length > 0 ? args[0] : "127.0.0.1";
int port = args.Length > 1 ? int.Parse(args[1]) : 7777;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

var hostBuilder = Host.CreateApplicationBuilder();
hostBuilder.AddFactoryServerApiServices();
var hostApp = hostBuilder.Build();

IFactoryServerClientFactory factory = hostApp.Services.GetRequiredService<IFactoryServerClientFactory>();
IFactoryServerClient client = await factory.BuildFactoryServerClientAsync(host, port);

var hb = new Heartbeat();
client.ServerStateReceived += (_, s) => hb.Record(s);
client.ErrorOccurred += (_, e) => hb.Error(e.Message);

// Start polling right away so the heartbeat pane is alive from the start.
// NOTE: StartPollingAsync returns the long-running poll loop — never await it.
bool wantPolling = true;
bool IsPollingActive() => wantPolling && hb.IsLive();
try { _ = client.StartPollingAsync(); } catch { /* server may be down */ }

bool IsAdmin() => client.CurrentPrivilegeLevel is FactoryServerPrivilegeLevel.Administrator or FactoryServerPrivilegeLevel.APIToken;
bool IsAuthenticated() => client.CurrentPrivilegeLevel is not FactoryServerPrivilegeLevel.NotAuthenticated;

static string ErrText(FactoryServerError? e) => e is null ? "ok" : $"{e.ErrorCode} - {e.ErrorMessage}";

// ---------------------------------------------------------------------------
//  Terminal.Gui application
// ---------------------------------------------------------------------------

var app = Application.Create(new SystemTimeProvider());
app.Init(); // let Terminal.Gui pick the best driver for this terminal

var rightView = new View
{
    X = Pos.Absolute(42),
    Y = Pos.Absolute(0),
    Width = Dim.Fill(Dim.Absolute(2)),
    Height = Dim.Fill(),
};

// ---------------------------------------------------------------------------
//  Right-pane swapping: main menu -> section -> prompt -> section -> ...
// ---------------------------------------------------------------------------

View currentPane = null!;
var paneStack = new Stack<View>();
var paneFocus = new Dictionary<View, Action>();

void ShowPane(View v)
{
    if (currentPane is not null) rightView.Remove(currentPane);
    rightView.Add(v);
    currentPane = v;
}

void PushPane(View v, Action? focus = null)
{
    paneStack.Push(currentPane);
    if (focus is not null) paneFocus[v] = focus;
    ShowPane(v);
}

void PopPane()
{
    var v = paneStack.Pop();
    if (paneFocus.Remove(v, out var focus)) focus();
    ShowPane(v);
}

// ---------------------------------------------------------------------------
//  Message log (bottom of the main pane)
// ---------------------------------------------------------------------------

var messages = new List<string>();
Label msgLabel = new()
{
    Text = "",
    X = Pos.Absolute(1),
    Y = Pos.Absolute(20),
    Width = Dim.Fill(Dim.Absolute(2)),
    Height = Dim.Fill(Dim.Absolute(4)),
};

void Msg(string text)
{
    lock (messages)
    {
        messages.Add(text);
        if (messages.Count > 8) messages.RemoveAt(0);
        msgLabel.Text = string.Join("\n", messages);
    }
}

// ---------------------------------------------------------------------------
//  Prompt panes (text input / choice)
// ---------------------------------------------------------------------------

async Task<string?> AskText(string title, string prompt, bool password = false)
{
    var tcs = new TaskCompletionSource<string?>();
    var pane = new View
    {
        X = Pos.Absolute(0),
        Y = Pos.Absolute(0),
        Width = Dim.Fill(),
        Height = Dim.Fill(),
    };
    var promptL = new Label
    {
        Text = prompt,
        X = Pos.Absolute(1),
        Y = Pos.Absolute(1),
        Width = Dim.Fill(Dim.Absolute(2)),
    };
    var field = new TextField
    {
        X = Pos.Absolute(1),
        Y = Pos.Absolute(3),
        Width = Dim.Absolute(44),
        Secret = password,
    };
    var hintL = new Label
    {
        Text = "enter = ok, tab to cancel = abort",
        X = Pos.Absolute(1),
        Y = Pos.Absolute(5),
        Width = Dim.Fill(Dim.Absolute(2)),
    };
    var cancel = new Button
    {
        Title = "cancel",
        X = Pos.Absolute(1),
        Y = Pos.Absolute(7),
        Width = Dim.Absolute(16),
    };
    field.Accepted += (_, _) => tcs.TrySetResult(field.Text);
    cancel.Accepted += (_, _) => tcs.TrySetResult(null);
    pane.Add(promptL);
    pane.Add(field);
    pane.Add(hintL);
    pane.Add(cancel);
    PushPane(pane, () => field.SetFocus());
    string? r = await tcs.Task;
    PopPane();
    return r;
}

async Task<int> AskChoice(string title, string prompt, string[] options)
{
    var tcs = new TaskCompletionSource<int>();
    var pane = new View
    {
        X = Pos.Absolute(0),
        Y = Pos.Absolute(0),
        Width = Dim.Fill(),
        Height = Dim.Fill(),
    };
    var promptL = new Label
    {
        Text = prompt,
        X = Pos.Absolute(1),
        Y = Pos.Absolute(1),
        Width = Dim.Fill(Dim.Absolute(2)),
    };
    pane.Add(promptL);
    int y = 3;
    Button? first = null;
    for (int i = 0; i < options.Length; i++)
    {
        var b = new Button
        {
            Title = $"  {i + 1}. {options[i]}",
            X = Pos.Absolute(1),
            Y = Pos.Absolute(y),
            Width = Dim.Fill(Dim.Absolute(2)),
        };
        int idx = i + 1;
        b.Accepted += (_, _) => tcs.TrySetResult(idx);
        pane.Add(b);
        if (first is null) first = b;
        y += 2;
    }
    var cancel = new Button
    {
        Title = "cancel",
        X = Pos.Absolute(1),
        Y = Pos.Absolute(y + 1),
        Width = Dim.Absolute(16),
    };
    cancel.Accepted += (_, _) => tcs.TrySetResult(0);
    pane.Add(cancel);
    PushPane(pane, () => first?.SetFocus());
    int r = await tcs.Task;
    PopPane();
    return r;
}

// ---------------------------------------------------------------------------
//  Section panes (menus are rebuilt on every open, so they always reflect
//  the current server/auth/polling state)
// ---------------------------------------------------------------------------

void OpenSection(string title, Func<List<(string Label, Func<Task> Act)>> itemsFactory)
{
    var items = itemsFactory();
    var pane = new View
    {
        X = Pos.Absolute(0),
        Y = Pos.Absolute(0),
        Width = Dim.Fill(),
        Height = Dim.Fill(),
    };
    var head = new Label
    {
        Text = title,
        X = Pos.Absolute(1),
        Y = Pos.Absolute(1),
        Width = Dim.Fill(Dim.Absolute(2)),
    };
    pane.Add(head);
    var status = new Label
    {
        Text = items.Count == 0 ? "(nothing available in the current state)" : "",
        X = Pos.Absolute(1),
        Y = Pos.Absolute(3),
        Width = Dim.Fill(Dim.Absolute(2)),
    };
    pane.Add(status);

    var buttons = new List<Button>();
    int y = 5;
    foreach (var (label, act) in items)
    {
        var b = new Button
        {
            Title = $"  {label}",
            X = Pos.Absolute(1),
            Y = Pos.Absolute(y),
            Width = Dim.Fill(Dim.Absolute(2)),
        };
        b.Accepted += async (_, _) =>
        {
            foreach (var c in buttons) c.Enabled = false;
            status.Text = "working ...";
            try
            {
                await act();
                status.Text = "";
            }
            catch (Exception ex)
            {
                Msg($"EXCEPTION: {ex.Message}");
                status.Text = "";
            }
            finally
            {
                foreach (var c in buttons) c.Enabled = true;
                try { b.SetFocus(); } catch { /* pane may be gone */ }
            }
        };
        pane.Add(b);
        buttons.Add(b);
        y += 2;
    }
    var back = new Button
    {
        Title = "back",
        X = Pos.Absolute(1),
        Y = Pos.Absolute(y + 1),
        Width = Dim.Absolute(16),
    };
    back.Accepted += (_, _) => PopPane();
    pane.Add(back);
    PushPane(pane, () => (buttons.Count > 0 ? buttons[0] : back).SetFocus());
}

// ---------------------------------------------------------------------------
//  Main layout
// ---------------------------------------------------------------------------

Label infoLabel = new()
{
    Text = $"server {host}:{port}",
    X = Pos.Absolute(1),
    Y = Pos.Absolute(1),
    Width = Dim.Fill(Dim.Absolute(2)),
};

var mainPane = new View
{
    X = Pos.Absolute(0),
    Y = Pos.Absolute(0),
    Width = Dim.Fill(),
    Height = Dim.Fill(),
};
mainPane.Add(infoLabel);

int y = 4;
Button SectionButton(string label, Action open)
{
    var b = new Button
    {
        Title = $"  {label}",
        X = Pos.Absolute(1),
        Y = Pos.Absolute(y),
        Width = Dim.Fill(Dim.Absolute(2)),
    };
    b.Accepted += (_, _) => open();
    mainPane.Add(b);
    y += 2;
    return b;
}

SectionButton("server status", () => OpenSection("Server status", StatusItems));
SectionButton("authentication", () => OpenSection("Authentication", AuthItems));
SectionButton("admin", () => OpenSection("Admin", AdminItems));
SectionButton("game / saves", () => OpenSection("Game / saves", GameItems));
SectionButton("udp", () => OpenSection("UDP polling", UdpItems));
SectionButton("player", () => OpenSection("Player", PlayerItems));
var quit = SectionButton("quit", () => app.RequestStop());

mainPane.Add(msgLabel);

var hbLabel = new Label
{
    Text = "",
    X = Pos.Absolute(1),
    Y = Pos.Absolute(1),
    Width = Dim.Absolute(38),
    Height = Dim.Fill(Dim.Absolute(2)),
};
var leftView = new View
{
    X = Pos.Absolute(0),
    Y = Pos.Absolute(0),
    Width = Dim.Absolute(40),
    Height = Dim.Fill(),
};
leftView.Add(hbLabel);

ShowPane(mainPane);
paneFocus[mainPane] = () => { };

var root = new Window { Title = "FactoryServerApi" };
root.Add(leftView);
root.Add(rightView);

// ---------------------------------------------------------------------------
//  Heartbeat refresh (every 500 ms, on the main thread)
// ---------------------------------------------------------------------------

string HbText()
{
    var (history, last, lastAt, polls, lastError) = hb.Snapshot();
    string age = last is null ? "never"
        : $"{(int)(DateTime.Now - lastAt).TotalSeconds}s ago";

    List<string> lines =
    [
        " UDP HEARTBEAT",
        $"  server  {host}:{port}",
        $"  state   {(last is null ? "—" : last.ServerState.ToString())}",
        $"  name    \"{last?.ServerName ?? "—"}\"",
        $"  netCL   {(last?.ServerNetCL.ToString() ?? "—")}  flags {(last is null ? "—" : last.ServerFlags.ToString("X"))}",
        $"  polls   {polls}",
        $"  last    {(last is null ? "—" : last.ReceivedUtc.ToLocalTime().ToString("HH:mm:ss"))}  ({age})",
        $"  err     {Trunc(lastError, 32)}",
        " history",
    ];
    foreach (var (time, state) in history)
        lines.Add($"  {time}  {state}");
    return string.Join("\n", lines);
}

static string Trunc(string s, int width) => s.Length <= width ? s : s[..(width - 3)] + "...";

bool Tick()
{
    try
    {
        string info = $"server {host}:{port}   state {(client.CurrentServerState is null ? "—" : client.CurrentServerState.ServerState.ToString())}" +
                      $"   priv {client.CurrentPrivilegeLevel}   claimed {client.IsClaimed}";
        infoLabel.Text = info;
        hbLabel.Text = HbText();
    }
    catch (Exception ex)
    {
        infoLabel.Text = $"server {host}:{port}   (refresh error: {ex.Message})";
    }
    app.AddTimeout(TimeSpan.FromMilliseconds(500), Tick);
    return true;
}
app.AddTimeout(TimeSpan.FromMilliseconds(500), Tick);

// ---------------------------------------------------------------------------
//  Section item factories (state-aware)
// ---------------------------------------------------------------------------

List<(string, Func<Task>)> StatusItems() =>
[
    ("health check", async () =>
    {
        string? custom = await AskText("health check", "client custom data (blank = none):");
        if (custom is null) return;
        var r = await client.HealthCheckAsync(custom.Length > 0 ? custom : null);
        Msg(r.Error is null ? $"healthy: {r.Data?.Health} customData={r.Data?.ServerCustomData}" : ErrText(r.Error));
    }),
    ("is server online?", async () =>
    {
        bool online = await client.GetIsServerOnlineAsync(TimeSpan.FromSeconds(5));
        Msg($"server online: {online}");
    }),
    ("udp state (single poll)", async () =>
    {
        var st = await client.GetServerStateAsync();
        Msg(st is null ? "no udp response"
            : $"state={st.ServerState} name=\"{st.ServerName}\" netCL={st.ServerNetCL} flags={st.ServerFlags:X} subStates={st.SubStates.Count}");
    }),
    ("query server state (https)", async () =>
    {
        var r = await client.QueryServerStateAsync();
        if (r.Error is not null) { Msg(ErrText(r.Error)); return; }
        ServerGameState g = r.Data!.ServerGameState;
        Msg($"phase={g.GamePhase} running={g.IsGameRunning} paused={g.IsGamePaused}");
        Msg($"session=\"{g.ActiveSessionName}\" autoLoad=\"{g.AutoLoadSessionName}\" schematic={g.ActiveSchematic}");
        Msg($"players={g.NumConnectedPlayers}/{g.PlayerLimit} techTier={g.TechTier} ticks={g.AverageTickRate} duration={g.TotalGameDuration}s");
    }),
    ("server options", async () =>
    {
        var r = await client.GetServerOptionsAsync();
        if (r.Error is not null) { Msg(ErrText(r.Error)); return; }
        foreach (var (k, v) in r.Data!.ServerOptions)
            Msg($"{k} = {v}");
        if (r.Data.PendingServerOptions.Count > 0)
            foreach (var (k, v) in r.Data.PendingServerOptions)
                Msg($"pending: {k} = {v}");
    }),
    ("advanced game settings", async () =>
    {
        var r = await client.GetAdvancedGameSettingsAsync();
        if (r.Error is not null) { Msg(ErrText(r.Error)); return; }
        Msg($"creative={r.Data!.CreativeModeEnabled}");
        foreach (var (k, v) in r.Data.AdvancedGameSettings)
            Msg($"{k} = {v}");
    }),
];

List<(string, Func<Task>)> AuthItems()
{
    var m = new List<(string, Func<Task>)>
    {
        ("initialize (claim if needed)", async () =>
        {
            await client.InitializeAsync();
            if (client.MustClaim)
            {
                string? name = await AskText("claim server", "server name to claim with:");
                string? pw = await AskText("claim server", "admin password:", password: true);
                if (name is null || pw is null) { Msg("claim skipped"); return; }
                Msg(await client.ClaimServerAsync(name, pw.AsMemory()) ? "server claimed" : "claim failed");
            }
            if (client.CurrentPrivilegeLevel is not (FactoryServerPrivilegeLevel.Administrator or FactoryServerPrivilegeLevel.APIToken))
            {
                if (!await client.EnsureAdminAuthenticationAsync())
                {
                    string? pw = await AskText("admin login", "admin password:", password: true);
                    if (pw is not null)
                        Msg(await client.AdministratorLoginAsync(pw.AsMemory())
                            ? $"admin ok ({client.CurrentPrivilegeLevel})" : "admin login failed");
                }
            }
            Msg($"privilege now: {client.CurrentPrivilegeLevel}");
        }),
    };
    if (!client.IsClaimed)
        m.Add(("claim server", async () =>
        {
            string? name = await AskText("claim server", "server name:");
            string? pw = await AskText("claim server", "admin password:", password: true);
            if (name is null || pw is null) return;
            Msg(await client.ClaimServerAsync(name, pw.AsMemory()) ? "server claimed" : "claim failed");
        }));
    if (!IsAdmin())
    {
        m.Add(("admin login", async () =>
        {
            string? pw = await AskText("admin login", "admin password:", password: true);
            if (pw is null) return;
            Msg(await client.AdministratorLoginAsync(pw.AsMemory())
                ? $"admin ok ({client.CurrentPrivilegeLevel})" : "admin login failed");
        }));
        m.Add(("client login", async () =>
        {
            int mode = await AskChoice("client login", "login mode:", ["passwordless", "with password"]);
            if (mode == 0) return;
            string? pw = mode == 2 ? await AskText("client login", "client password:", password: true) : null;
            if (mode == 2 && pw is null) return;
            Msg(await client.ClientLoginAsync(mode == 2 ? pw.AsMemory() : null)
                ? $"client ok ({client.CurrentPrivilegeLevel})" : "client login failed");
        }));
        m.Add(("ensure admin", async () =>
        {
            Msg(await client.EnsureAdminAuthenticationAsync()
                ? $"admin ok ({client.CurrentPrivilegeLevel})" : "cannot restore admin (no persisted password)");
        }));
    }
    if (IsAdmin())
    {
        m.Add(("set admin password", async () =>
        {
            string? pw = await AskText("set admin password", "new admin password:", password: true);
            string? token = await AskText("set admin password", "authentication token to use afterwards:");
            if (pw is null || token is null) return;
            Msg(await client.SetAdminPasswordAsync(pw.AsMemory(), token) ? "admin password set" : "set admin password failed");
        }));
        m.Add(("set/clear client password", async () =>
        {
            int mode = await AskChoice("client password", "mode:", ["set password", "clear password"]);
            if (mode == 0) return;
            string? pw = null;
            if (mode == 1)
            {
                pw = await AskText("client password", "client password:", password: true);
                if (pw is null) return;
            }
            Msg(await client.SetClientPasswordAsync(pw is null ? null : pw.AsMemory()) is null ? "done" : "failed");
        }));
        m.Add(("generate api token", async () =>
        {
            var r = await client.RunCommandAsync("server.GenerateAPIToken");
            Msg(r.Error is null ? $"result: {r.Data?.CommandResult}" : ErrText(r.Error));
        }));
    }
    m.Add(("verify token", async () =>
    {
        bool valid = await client.GetIsAuthenticationTokenValidAsync();
        Msg($"token valid: {valid} ({client.CurrentPrivilegeLevel})");
    }));
    m.Add(("set api token", async () =>
    {
        string? token = await AskText("set api token", "api token:");
        if (token is null) return;
        await client.SetAuthenticationTokenAsync(token);
        Msg($"token set — privilege: {client.CurrentPrivilegeLevel}");
    }));
    if (IsAuthenticated())
        m.Add(("clear login data", async () =>
        {
            await client.ClearLoginDataAsync();
            Msg($"login data cleared — privilege: {client.CurrentPrivilegeLevel}");
        }));
    return m;
}

List<(string, Func<Task>)> AdminItems()
{
    var m = new List<(string, Func<Task>)>();
    if (IsAdmin())
    {
        m.Add(("rename server", async () =>
        {
            string? name = await AskText("rename server", "new server name:");
            if (name is null) return;
            Msg(await client.RenameServerAsync(name) is null ? "renamed" : "rename failed");
        }));
        m.Add(("set auto-load session", async () =>
        {
            string? name = await AskText("auto-load session", "session name:");
            if (name is null) return;
            Msg(await client.SetAutoLoadSessionNameAsync(name) is null ? "auto-load session set" : "set auto-load session failed");
        }));
        m.Add(("run command", async () =>
        {
            string? cmd = await AskText("run command", "console command:");
            if (cmd is null) return;
            var r = await client.RunCommandAsync(cmd);
            Msg(r.Error is null ? $"result: {r.Data?.CommandResult}" : ErrText(r.Error));
        }));
        m.Add(("apply server option", async () =>
        {
            var r = await client.GetServerOptionsAsync();
            if (r.Error is not null) { Msg(ErrText(r.Error)); return; }
            var options = r.Data!.ServerOptions;
            int i = await AskChoice("server options", "pick option to update:", options.Select(kv => kv.Key).ToArray());
            if (i == 0) return;
            var (key, old) = options.ElementAt(i - 1);
            string? value = await AskText("apply option", $"new value for {key} (was: {old}):");
            if (value is null) return;
            var err = await client.ApplyServerOptionsAsync(new Dictionary<string, string> { [key] = value });
            Msg(err is null ? $"applied {key} = {value}" : ErrText(err));
        }));
        m.Add(("apply advanced setting", async () =>
        {
            var r = await client.GetAdvancedGameSettingsAsync();
            if (r.Error is not null) { Msg(ErrText(r.Error)); return; }
            var settings = r.Data!.AdvancedGameSettings;
            int i = await AskChoice("advanced settings", "pick setting to update:", settings.Select(kv => kv.Key).ToArray());
            if (i == 0) return;
            var (key, old) = settings.ElementAt(i - 1);
            string? value = await AskText("apply setting", $"new value for {key} (was: {old}):");
            if (value is null) return;
            var err = await client.ApplyAdvancedGameSettingsAsync(new Dictionary<string, string> { [key] = value });
            Msg(err is null ? $"applied {key} = {value}" : ErrText(err));
        }));
        m.Add(("shutdown server", async () =>
        {
            int c = await AskChoice("shutdown", "are you sure?", ["yes, shutdown", "no"]);
            if (c != 1) { Msg("aborted"); return; }
            var err = await client.ShutdownAsync();
            Msg(err is null ? "shutdown requested" : ErrText(err));
        }));
    }
    return m;
}

List<(string, Func<Task>)> GameItems()
{
    var m = new List<(string, Func<Task>)>();
    if (IsAdmin())
    {
        m.Add(("create new game", async () =>
        {
            string? name = await AskText("new game", "session name:");
            if (name is null) return;
            int map = await AskChoice("new game", "map:", ["persistent level", "custom map"]);
            if (map == 0) return;
            MapName? mapName = map == 2
                ? MapName.CustomMap((await AskText("new game", "map name:")) ?? "")
                : MapName.PersistentLevel;
            int loc = await AskChoice("new game", "starting location:",
                ["random/none", "grass fields", "rocky desert", "northern forest", "dune desert", "custom"]);
            StartingLocation? location = loc switch
            {
                2 => StartingLocation.GrassFields,
                3 => StartingLocation.RockyDesert,
                4 => StartingLocation.NorthernForest,
                5 => StartingLocation.DuneDesert,
                6 => StartingLocation.CustomLocation((await AskText("new game", "location:")) ?? ""),
                _ => null,
            };
            int skip = await AskChoice("new game", "skip onboarding?", ["yes", "no"]);
            var err = await client.CreateNewGameAsync(new ServerNewGameData(name, mapName, location, skip == 1));
            Msg(err is null ? $"game created: {name}" : ErrText(err));
        }));
        m.Add(("save game", async () =>
        {
            string? name = await AskText("save game", "save name:");
            if (name is null) return;
            Msg(await client.SaveGameAsync(name) is null ? $"saved: {name}" : "save failed");
        }));
        m.Add(("load game", async () =>
        {
            string? name = await AskText("load game", "save name to load:");
            if (name is null) return;
            int adv = await AskChoice("load game", "enable advanced game settings?", ["yes", "no"]);
            Msg(await client.LoadGameAsync(name, adv == 1) is null ? $"loading: {name}" : "load failed");
        }));
        m.Add(("upload save <- file", async () =>
        {
            string? file = await AskText("upload save", "save file to upload:");
            if (file is null || !File.Exists(file)) { Msg($"file not found: {file}"); return; }
            string? name = await AskText("upload save", "save name on the server:");
            if (name is null) return;
            int load = await AskChoice("upload save", "load the save after upload?", ["yes", "no"]);
            int adv = await AskChoice("upload save", "enable advanced game settings?", ["yes", "no"]);
            using FileStream fs = File.OpenRead(file);
            Msg(await client.UploadSaveGameAsync(name, load == 1, adv == 1, fs) is null
                ? $"uploaded {file} as {name}" : "upload failed");
        }));
        m.Add(("delete save file", async () =>
        {
            string? name = await AskText("delete save", "save name to delete:");
            if (name is null) return;
            Msg(await client.DeleteSaveFileAsync(name) is null ? $"deleted save: {name}" : "delete save failed");
        }));
        m.Add(("delete save session", async () =>
        {
            string? name = await AskText("delete session", "session to delete:");
            if (name is null) return;
            Msg(await client.DeleteSaveSessionAsync(name) is null ? $"deleted session: {name}" : "delete save failed session");
        }));
    }
    m.Add(("enumerate sessions", async () =>
    {
        var r = await client.EnumerateSessionsAsync();
        if (r.Error is not null) { Msg(ErrText(r.Error)); return; }
        Msg($"sessions ({r.Data!.Sessions.Count}), current index {r.Data.CurrentSessionIndex}:");
        foreach (var s in r.Data.Sessions)
        {
            Msg($"• {s.SessionName} ({s.SaveHeaders.Count} saves)");
            foreach (var f in s.SaveHeaders)
                Msg($"    {f.SaveName} — {f.SaveDateTime} — {f.PlayDurationSeconds}s — {f.MapName}" +
                    (f.IsModdedSave ? " [modded]" : "") + (f.IsCreativeModeEnabled ? " [creative]" : ""));
        }
    }));
    m.Add(("download save -> file", async () =>
    {
        string? name = await AskText("download save", "save name to download:");
        if (name is null) return;
        string? path = await AskText("download save", $"destination file (blank: download_{name}.bin):");
        if (path is null) return;
        if (path.Length == 0) path = $"download_{name}.bin";
        var r = await client.DownloadSaveGameAsync(name);
        if (r.Error is not null) { Msg(ErrText(r.Error)); return; }
        await using FileStream fs = File.Create(path);
        await r.Data!.SaveFileStream.CopyToAsync(fs);
        Msg($"downloaded {path} ({new FileInfo(path).Length:N0} bytes)");
    }));
    return m;
}

List<(string, Func<Task>)> UdpItems()
{
    var m = new List<(string, Func<Task>)>();
    if (IsPollingActive())
        m.Add(("stop udp polling", async () =>
        {
            await client.StopPollingAsync();
            wantPolling = false;
            Msg("polling stopped");
        }));
    else
        m.Add(("start udp polling", async () =>
        {
            _ = client.StartPollingAsync();
            wantPolling = true;
            Msg("polling started");
        }));
    return m;
}

List<(string, Func<Task>)> PlayerItems()
{
    var m = new List<(string, Func<Task>)>
    {
        ("set player id (steam)", async () =>
        {
            string? id = await AskText("player id", "steam id64 (decimal):");
            if (id is null) return;
            if (ulong.TryParse(id, out ulong steamId))
            {
                await client.SetPlayerIdAsync(new FactoryGamePlayerId(steamId));
                Msg($"player id set: {client.PlayerId}");
            }
            else Msg("invalid steam id");
        }),
        ("set player id (epic)", async () =>
        {
            string? id = await AskText("player id", "epic id (hex):");
            if (id is null || id.Length == 0) return;
            await client.SetPlayerIdAsync(new FactoryGamePlayerId(id));
            Msg($"player id set: {client.PlayerId}");
        }),
    };
    if (client.PlayerId is not null)
        m.Add(("clear player id", async () =>
        {
            await client.ClearPlayerIdAsync();
            Msg("player id cleared");
        }));
    return m;
}

// ---------------------------------------------------------------------------
//  Run
// ---------------------------------------------------------------------------

try
{
    // exceptionHandler: report the exception in the UI and keep the app alive,
    // instead of letting it kill the run loop (frozen screen).
    app.Run(root, ex =>
    {
        Msg($"TUI EXCEPTION: {ex.Message}");
        return true; // continue running
    });
}
catch (Exception ex)
{
    Console.Error.WriteLine($"TUI failed to start: {ex}");
}
finally
{
    client.Dispose();
}

// ---------------------------------------------------------------------------
//  Heartbeat model
// ---------------------------------------------------------------------------

file class Heartbeat
{
    readonly object _lock = new();
    readonly List<(string Time, string State)> _history = new();
    FactoryServerStateUdpResponse? _last;
    DateTime _lastAt;
    int _polls;
    string _lastError = "";

    public void Record(FactoryServerStateUdpResponse s)
    {
        lock (_lock)
        {
            _last = s;
            _lastAt = DateTime.Now;
            _polls++;
            _history.Insert(0, (s.ReceivedUtc.ToLocalTime().ToString("HH:mm:ss"), s.ServerState.ToString()));
            if (_history.Count > 15) _history.RemoveAt(_history.Count - 1);
            _lastError = "";
        }
    }

    public void Error(string msg)
    {
        lock (_lock) _lastError = msg;
    }

    public bool IsLive() => _last is not null && (DateTime.Now - _lastAt).TotalSeconds < 8;

    public (List<(string Time, string State)> History,
            FactoryServerStateUdpResponse? Last,
            DateTime LastAt,
            int Polls,
            string LastError) Snapshot()
    {
        lock (_lock)
            return (new List<(string, string)>(_history), _last, _lastAt, _polls, _lastError);
    }
}
