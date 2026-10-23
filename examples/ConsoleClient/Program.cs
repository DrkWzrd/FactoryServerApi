using FactoryServerApi;
using FactoryServerApi.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Usage:
//   ConsoleClient [host] [port] [--claim <name> <password>]
//                 [--admin-login <password>] [--create-game <sessionName>] [--duration <seconds>]
//
// Both the HTTPS API and the UDP Lightweight Query endpoint listen on the game server port.
//
// Read-only by default. --claim / --admin-login / --create-game are opt-in, state-changing operations.

string host = "127.0.0.1";
int port = 7777;
string? claimName = null, claimPassword = null;
string? adminPassword = null;
string? createGameName = null;
int durationSeconds = 90;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--claim": claimName = args[++i]; claimPassword = args[++i]; break;
        case "--admin-login": adminPassword = args[++i]; break;
        case "--create-game": createGameName = args[++i]; break;
        case "--duration": durationSeconds = int.Parse(args[++i]); break;
        default:
            if (host == "127.0.0.1" && i == 0) host = args[i];
            else if (port == 7777 && i == 1) port = int.Parse(args[i]);
            else { Console.WriteLine($"Unknown arg: {args[i]}"); return 2; }
            break;
    }
}

// Run from the app directory so the optional settings file (copied to the
// output) is found regardless of the working directory the app was started in.
Directory.SetCurrentDirectory(AppContext.BaseDirectory);

var builder = Host.CreateApplicationBuilder();
builder.AddFactoryServerServices();
var app = builder.Build();

var client = await app.Services
    .GetRequiredService<IFactoryServerClientFactory>()
    .BuildFactoryServerClientAsync(host, port);

Console.WriteLine($"[*] client built: {host}:{port} (http + udp)");

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(durationSeconds + 120));
var ct = cts.Token;

DateTimeOffset stamp() => DateTimeOffset.Now;
client.ServerStateChanged += (_, e) =>
{
    var s = e.ServerStateSnapshot;
    Console.WriteLine($"[{stamp():HH:mm:ss}] STATE state={s.ServerState} claimed={s.IsClaimed} name={s.ServerName ?? "-"} sessions={s.Sessions.Count} health={s.HealthState}");
};
client.ErrorOccurred += (_, e) =>
{
    var msg = e.Exception?.Message ?? e.Error?.ErrorMessage ?? "?";
    Console.WriteLine($"[{stamp():HH:mm:ss}] ERROR {e.Error?.ErrorCode}: {msg}");
};

await client.InitializeClientAsync(ct);
Console.WriteLine($"[*] initialized, waiting for UDP state...");

// Wait for the first non-offline UDP state (server startup / map loading can take a while).
FactoryServerInfoSnapshot snap = default!;
for (int i = 0; i < 300 && !ct.IsCancellationRequested; i++)
{
    snap = await client.GetCurrentServerStateAsync(ct);
    if (snap.ServerState != FactoryServerState.Offline)
        break;
    await Task.Delay(2000, ct);
}

Console.WriteLine($"[*] first state: {snap.ServerState} claimed={snap.IsClaimed}");
if (snap.ServerState == FactoryServerState.Offline)
{
    Console.WriteLine("[!] server never came up on UDP; aborting probes");
    client.Dispose();
    return 1;
}

var http = client.ApiAccessPoint;

// The protected endpoints require at least Client privilege: do a passwordless
// client login first (fails if the server has a client password set).
try
{
    await client.ClientLoginAsync(null, ct);
    string? tok = client.ApiAccessPoint.AuthenticationToken;
    Console.WriteLine($"[*] passwordless client login OK, privilege={client.ClientCurrentPrivilegeLevel}");
    Console.WriteLine($"    token= {(tok is null ? "null" : tok.Length > 60 ? tok[..60] + "..." : tok)}");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"[!] client login failed (server has a client password?): {ex.Message}");
}

// --- read-only probes ---------------------------------------------------------
Console.WriteLine($"--- GetIsServerOnlineAsync (udp+http) ---");
bool online = await client.GetIsServerOnlineAsync(TimeSpan.FromSeconds(10), checkUdp: true, ct);
Console.WriteLine($"    online={online}");

var health = await http.HealthCheckAsync("console-client-test", ct);
Console.WriteLine($"--- HealthCheck: error={health.Error?.ErrorCode ?? "null"} data={health.Data is not null}");

var state = await http.QueryServerStateAsync(ct);
Console.WriteLine($"--- QueryServerState: error={state.Error?.ErrorCode ?? "null"} data={state.Data is not null}");

var options = await http.GetServerOptionsAsync(ct);
Console.WriteLine($"--- GetServerOptions: error={options.Error?.ErrorCode ?? "null"} count={options.Data?.ServerOptions.Count}");

var adv = await http.GetAdvancedGameSettingsAsync(ct);
Console.WriteLine($"--- GetAdvancedGameSettings: error={adv.Error?.ErrorCode ?? "null"} count={adv.Data?.AdvancedGameSettings.Count}");

var sessions = await http.EnumerateSessionsAsync(ct);
Console.WriteLine($"--- EnumerateSessions: error={sessions.Error?.ErrorCode ?? "null"} count={sessions.Data?.Sessions.Count} current={sessions.Data?.CurrentSessionIndex}");
if (sessions.Data is not null)
{
    foreach (var session in sessions.Data.Sessions)
    {
        Console.WriteLine($"    session: {session.SessionName} ({session.SaveHeaders.Count} saves)");
        if (session.SaveHeaders.Count > 0)
        {
            var saveName = session.SaveHeaders[0].SaveName;
            Console.WriteLine($"    downloading save: {saveName}");
            try
            {
                var download = await http.DownloadSaveGameAsync(saveName, ct);
                if (download.Data is not null)
                {
                    long len = 0;
                    using (var fs = download.Data.SaveFileStream)
                    {
                        len = fs.Length;
                    }
                    Console.WriteLine($"    download OK: {len:N0} bytes (file response via Content-Disposition)");
                }
                else
                {
                    Console.WriteLine($"    download got JSON: error={download.Error?.ErrorCode ?? "null"}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    download FAILED: {ex.Message}");
            }
        }
    }
}

// --- opt-in state-changing operations -----------------------------------------
if (adminPassword is not null)
{
    Console.WriteLine($"--- AdministratorLogin ---");
    await client.AdministratorLoginAsync(adminPassword.AsMemory(), ct);
    Console.WriteLine($"    privilege={client.ClientCurrentPrivilegeLevel}");
}

if (claimName is not null && claimPassword is not null)
{
    Console.WriteLine($"--- ClaimServer: {claimName} ---");
    try
    {
        await client.ClaimServerAsync(claimName, claimPassword.AsMemory(), ct);
        Console.WriteLine($"    claimed. privilege={client.ClientCurrentPrivilegeLevel}");
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine($"    claim failed: {ex.Message}");
    }
}

if (createGameName is not null)
{
    Console.WriteLine($"--- CreateNewGame: {createGameName} ---");
    var newGame = new ServerNewGameData(createGameName)
    {
        SkipOnboarding = true,
    };
    var createError = await http.CreateNewGameAsync(newGame, ct);
    Console.WriteLine($"    create: error={createError?.ErrorCode ?? "null"} {createError?.ErrorMessage}");

    // Watch the Loading -> Playing transition.
    Console.WriteLine("[*] watching state while the map loads...");
    for (int i = 0; i < 600 && !ct.IsCancellationRequested; i++)
    {
        snap = await client.GetCurrentServerStateAsync(ct);
        if (snap.ServerState == FactoryServerState.Playing)
            break;
        await Task.Delay(2000, ct);
    }
    Console.WriteLine($"    final state: {snap.ServerState}");
}

Console.WriteLine($"[*] observing for the remaining time...");
await Task.Delay(15000, ct);
client.Dispose();
Console.WriteLine("[*] done");
return 0;
