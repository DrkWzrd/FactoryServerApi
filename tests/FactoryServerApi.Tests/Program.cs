//using FactoryServerApi;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;

//Directory.SetCurrentDirectory(AppContext.BaseDirectory);
//var builder = Host.CreateApplicationBuilder();
//builder.AddFactoryServerServices();
//var app = builder.Build();

//const string AdminPassword = "FactoryTest2026!";
//const string ClientPassword = "clientTest42";

//var client = await app.Services.GetRequiredService<IFactoryServerClientFactory>()
//    .BuildFactoryServerClientAsync("127.0.0.1", 7777);
//client.ErrorOccurred += (s, e) => Console.WriteLine($"    [error event] {e.Error?.ErrorCode}: {e.Error?.ErrorMessage}");
//client.ServerStateChanged += (s, e) => Console.WriteLine($"    [state event] {e.ServerStateSnapshot.ServerState}");
//var http = client.ApiAccessPoint;
//var failures = 0;
//void Check(string name, bool ok, string detail = "")
//{
//    Console.WriteLine($"[{(ok ? "PASS" : "FAIL")}] {name}{(detail.Length > 0 ? " - " + detail : "")}");
//    if (!ok) failures++;
//}
//async Task<bool> Step(string name, Func<Task> action)
//{
//    Console.WriteLine($"\n>> {name}");
//    try { await action(); return true; }
//    catch (Exception ex) { Check(name, false, $"{ex.GetType().Name}: {ex.Message}"); return false; }
//}

//try
//{
//    // ---- 0. Initialize ----
//    await client.InitializeClientAsync();
//    FactoryServerInfoSnapshot info = await client.GetCurrentServerStateAsync();
//    Console.WriteLine($"[*] state={info.ServerState} claimed={info.IsClaimed}");

//    if (info.IsClaimed ?? false)
//    {

//    }
//    else
//    {

//    }

//    // ---- 2. Get an admin token: claim (unclaimed) or admin login (claimed) ----
//    bool adminOk = false;
//    if (info.IsClaimed ?? false)
//    {
//        adminOk = await Step("ClaimServerAsync('TestWorld', adminPassword)", async () =>
//        {
//            await client.ClaimServerAsync("TestWorld", AdminPassword.AsMemory());
//            Check("claim", client.ClientCurrentPrivilegeLevel == FactoryServerPrivilegeLevel.Administrator,
//                $"privilege={client.ClientCurrentPrivilegeLevel}");
//        });
//        info = await client.GetCurrentServerStateAsync();
//        Check("claimed flag", info.IsClaimed == true, $"name={info.ServerName}");
//    }
//    else
//    {
//        adminOk = await Step("AdministratorLoginAsync(adminPassword)", async () =>
//        {
//            await client.ClearLoginDataAsync();
//            await client.AdministratorLoginAsync(AdminPassword.AsMemory());
//            Check("admin login", client.ClientCurrentPrivilegeLevel == FactoryServerPrivilegeLevel.Administrator,
//                $"privilege={client.ClientCurrentPrivilegeLevel}");
//        });
//    }
//    if (adminOk)
//        Check("admin token valid", await client.GetIsAuthenticationTokenValidAsync());

//    if (!adminOk)
//    {
//        Console.WriteLine("\n[ABORT] No admin access - cannot continue the flow.");
//        Console.WriteLine($"=== {failures} FAILURES (flow aborted) ===");
//        Environment.Exit(1);
//    }

//    // ---- 3. Client login flows ----
//    // Passwordless client login only works when NO client password is set.
//    await client.ClearLoginDataAsync();
//    var pwless = await http.PasswordlessLoginAsync(FactoryServerPrivilegeLevel.Client);
//    bool clientPasswordSet = pwless.Error is not null;
//    Console.WriteLine($"[*] client password {(clientPasswordSet ? "IS" : "is NOT")} set");

//    if (!clientPasswordSet)
//    {
//        await client.SetAuthenticationTokenAsync(pwless.Data!.AuthenticationToken);
//        Check("passwordless client login",
//            client.ClientCurrentPrivilegeLevel == FactoryServerPrivilegeLevel.Client,
//            $"privilege={client.ClientCurrentPrivilegeLevel}");

//        // Client privilege must be blocked from admin endpoints.
//        var blocked = await http.SetClientPasswordAsync(ClientPassword.AsMemory());
//        Check("client blocked from admin endpoint", blocked is not null, blocked?.ErrorCode ?? "no error");

//        // Set the client password as admin (known bug: char[] serialization).
//        await client.AdministratorLoginAsync(AdminPassword.AsMemory());
//        var setPw = await http.SetClientPasswordAsync(ClientPassword.AsMemory());
//        Check("set client password", setPw is null, setPw?.ErrorMessage ?? "ok");
//        if (setPw is null)
//        {
//            clientPasswordSet = true;
//        }
//    }

//    // ---- 4. Protected reads (admin) ----
//    await client.AdministratorLoginAsync(AdminPassword.AsMemory());
//    var q = await http.QueryServerStateAsync();
//    Check("query server state", q.Error is null && q.Data is not null);
//    var opts = await http.GetServerOptionsAsync();
//    Check("server options", opts.Error is null, $"count={opts.Data?.ServerOptions.Count}");
//    var adv = await http.GetAdvancedGameSettingsAsync();
//    Check("advanced game settings", adv.Error is null, $"count={adv.Data?.AdvancedGameSettings.Count}");
//    var sess = await http.EnumerateSessionsAsync();
//    Check("enumerate sessions", sess.Error is null, $"currentIndex={sess.Data?.CurrentSessionIndex}");

//    // ---- 5. Create game (known bug: missing GameModeSettings) ----
//    FactoryServerState? lastState = null;
//    bool playing = false;
//    var create = await http.CreateNewGameAsync(
//        new ServerNewGameData("FlowTest", MapName.PersistentLevel, StartingLocation.GrassFields, true));
//    Check("create new game", create is null, create?.ErrorMessage ?? "ok");
//    if (create is null)
//    {
//        Console.WriteLine("[*] waiting for state to become Playing (max 180s)...");
//        for (int i = 0; i < 90; i++)
//        {
//            info = await client.GetCurrentServerStateAsync();
//            if (info.ServerState != lastState)
//            {
//                Console.WriteLine($"    state change: {lastState} -> {info.ServerState}");
//                lastState = info.ServerState;
//            }
//            if (info.ServerState == FactoryServerState.Playing) { playing = true; break; }
//            await Task.Delay(2000);
//        }
//        Check("reached Playing", playing, $"lastState={lastState}");

//        // ---- 6. Save + download (only if a game is actually loaded) ----
//        if (playing)
//        {
//            var save = await http.SaveGameAsync("FlowSave1");
//            Check("save game", save is null, save?.ErrorMessage ?? "ok");
//            if (save is null)
//            {
//                var dl = await http.DownloadSaveGameAsync("FlowSave1");
//                long bytes = 0;
//                if (dl.Data?.SaveFileStream is not null)
//                    bytes = dl.Data.SaveFileStream.Length;
//                Check("download save", dl.Error is null && bytes > 0, $"{bytes} bytes");
//            }
//        }
//        else
//        {
//            Console.WriteLine("[SKIP] save/download - no game playing");
//        }
//    }
//    else
//    {
//        Console.WriteLine("[SKIP] save/download - game creation failed");
//    }

//    // ---- 7. RunCommand: generate an API token ----
//    await client.AdministratorLoginAsync(AdminPassword.AsMemory());
//    var rc = await http.RunCommandAsync("server.GenerateAPIToken");
//    Check("run command", rc.Error is null && rc.Data is not null,
//        rc.Data?.CommandResult ?? rc.Error?.ErrorMessage ?? "");

//    string? apiToken = null;
//    if (rc.Data?.CommandResult is not null)
//    {
//        var match = System.Text.RegularExpressions.Regex.Match(
//            rc.Data.CommandResult, @"([A-Za-z0-9+/=]+\.[0-9A-Fa-f]{16,})");
//        apiToken = match.Success ? match.Groups[1].Value : null;
//    }
//    if (apiToken is not null)
//    {
//        await client.SetAuthenticationTokenAsync(apiToken);
//        Check("api token privilege", client.ClientCurrentPrivilegeLevel == FactoryServerPrivilegeLevel.APIToken,
//            $"privilege={client.ClientCurrentPrivilegeLevel}");
//        Check("api token valid", await client.GetIsAuthenticationTokenValidAsync());
//        var h = await http.HealthCheckAsync(null);
//        Check("health check with api token", h.Error is null);
//    }
//    else
//    {
//        Check("extract api token", false, "no token found in command result");
//    }

//    // ---- 8. Cleanup: delete test session if it exists ----
//    await client.AdministratorLoginAsync(AdminPassword.AsMemory());
//    sess = await http.EnumerateSessionsAsync();
//    var flowTest = sess.Data?.Sessions.FirstOrDefault(s => s.SessionName == "FlowTest");
//    if (flowTest is not null)
//    {
//        var del = await http.DeleteSaveSessionAsync("FlowTest");
//        Check("delete save session", del is null, del?.ErrorMessage ?? "ok");
//    }
//    else
//    {
//        Console.WriteLine("[SKIP] delete session - FlowTest not present");
//    }

//    // ---- 9. Leave server in a known state ----
//    await client.ClearLoginDataAsync();
//    await client.AdministratorLoginAsync(AdminPassword.AsMemory());
//    Check("final admin login", client.ClientCurrentPrivilegeLevel == FactoryServerPrivilegeLevel.Administrator);
//}
//catch (Exception ex)
//{
//    Console.WriteLine($"[EXCEPTION] {ex.GetType().Name}: {ex.Message}");
//    Console.WriteLine(ex.StackTrace);
//    failures++;
//}
//finally
//{
//    client.Dispose();
//}

//Console.WriteLine($"\n=== {(failures == 0 ? "ALL PASSED" : failures + " FAILURES")} ===");
//Environment.Exit(failures == 0 ? 0 : 1);
