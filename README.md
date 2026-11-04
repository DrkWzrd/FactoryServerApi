### Simple Satisfactory Server Api for .net

An implementation of the documentation file of the Satisfactory dedicated server API, both UDP and HTTP.

It is integrated with the generic host and fully async. The repository contains the
library (`FactoryServerApi`) and a small WinForms UI (`FactoryServerUi`) built on top of it.

## Client (recommended entry point)

`IFactoryServerClient` combines UDP state polling with the HTTP API. It tracks the
last state received via UDP (`CurrentServerState`, raised as `ServerStateReceived`)
and exposes the full set of mutating operations (claim server, logins, sessions,
options, save files, ...). Admin functions are guarded: if the current token lacks
Administrator privileges, the client re-logs in with the last admin password used.
Disposing the client wipes the secrets it holds in memory.

```c#
var builder = Host.CreateApplicationBuilder(args);
builder.AddFactoryServerApiServices();

var app = builder.Build();

await using var client = await app.Services
    .GetRequiredService<IFactoryServerClientFactory>()
    .BuildFactoryServerClientAsync("192.168.1.10", 7777);

client.ServerStateReceived += (_, e) =>
{
    // e.ServerState, e.GameState, e.Sessions, ...
};

client.ErrorOccurred += (_, e) =>
{
    // e: Exception
};

// First action: is the server claimed?
FactoryServerClaimCheck claim = await client.CheckClaimStateAsync();

if (claim.Unclaimed)
{
    // Claim it: the InitialAdmin token is exchanged for a real admin token.
    await client.ClaimServerAsync("My Server", "adminPassword".AsMemory(), claim.InitialAdminToken);
}
else
{
    // Already claimed: log in with the admin password.
    await client.AdministratorLoginAsync("adminPassword".AsMemory());
}

_ = client.StartPollingAsync();
```

An optional API token (issued by the `server.GenerateAPIToken` console command) can be
passed to `BuildFactoryServerClientAsync` to authenticate as a third party application
without logging in.

## UDP

The UDP entry point `IFactoryServerUdpClient`, obtained from the factory
`IFactoryServerUdpClientFactory`:

```c#
var builder = Host.CreateApplicationBuilder(args);
builder.AddFactoryServerApiServices();

var app = builder.Build();

var udpFactory = app.Services.GetRequiredService<IFactoryServerUdpClientFactory>();

var udpClient = await udpFactory.BuildFactoryServerUdpClientAsync(host, port);

udpClient.ServerStateReceived += UdpClient_ServerStateReceived;
udpClient.ErrorOccurred += UdpClient_ErrorOccurred;

udpClient.StartPollingAsync(TimeSpan.FromMinutes(3));

// Or fetch a single state on demand:
var state = await udpClient.ReceiveMessageAsync(TimeSpan.FromSeconds(5));
```

## HTTP

The HTTP entry point `IFactoryServerHttpClient`, obtained from the factory
`IFactoryServerHttpClientFactory`:

```c#
var builder = Host.CreateApplicationBuilder(args);
builder.AddFactoryServerApiServices();

var app = builder.Build();

var httpClient = await app.Services
    .GetRequiredService<IFactoryServerHttpClientFactory>()
    .BuildFactoryServerHttpClientAsync("192.168.1.10", 7777);

var result = await httpClient.HealthCheckAsync(null);

string? json = null;
if (result.Data is not null)
{
    json = JsonSerializer.Serialize(result.Data);
}
else if (result.Error is not null)
{
    json = JsonSerializer.Serialize(result.Error);
}
```

The current authentication token is held in memory on the HTTP client
(`AuthenticationToken`, `SetAuthenticationTokenAsync`, `ClearAuthenticationTokenAsync`)
and wiped by `Dispose`.

## Configuration

Options are registered through `AddFactoryServerApiServices` with in-code defaults:

| Section | Option | Default |
| --- | --- | --- |
| `udpConfiguration` | `delayBetweenPolls` | `00:00:10` |
| | `messagesPerPoll` | `2` |
| | `timeoutRetriesBeforeStop` | `3` |
| | `protocolMagic` | `0xF6D5` |
| | `protocolVersion` | `1` |
| | `messageTermination` | `1` |
| `httpConfiguration` | `connectionTimeout` | `00:00:30` |
| | `apiPath` | `api/v1/` |
| `sslConfiguration` | `serverCertificateValidationStrategy` | `NoValidation` |

Optionally, a `FactoryServerApi.settings.json` file in the current working directory
can override any of these keys (the file is read if present, never written by the
library).

The `User-Agent` header is not configurable: it is derived from the assembly version
of the library.

### SSL certificate validation

The default `NoValidation` strategy accepts **any** server certificate, including
self-signed ones, without checking them. This is convenient for the common self-signed
setup of a home dedicated server, but it exposes the connection (including the admin
password and the authentication tokens) to man-in-the-middle attacks. For untrusted
networks set `serverCertificateValidationStrategy` to `Custom` and register an
`IServerCertificateValidationStrategy` implementation in the service collection.
With the `Custom` (and the default) strategy, certificate revocation lists are checked.

I'm new in this, and I made it as a hobby for managing remotely a server with my friends, don't be harsh.

Feel free to create issues for bugs and improvements, or fork.
