### Simple Satisfactory Server Api for .net

An implementation of the documentation file of the Satisfactory dedicated server API, both UDP and HTTP.

Is integrated with generic host and fully async.

## Client (recommended entry point)

`IFactoryServerClient` combines UDP state polling with the HTTP API: it keeps an up-to-date
`FactoryServerInfoSnapshot` (query it with `GetCurrentServerStateAsync`) and raises
`ServerStateChanged` when anything changes. It also exposes the full set of mutating operations
(create/claim server, logins, sessions, options, save files, ...).

```c#
var builder = Host.CreateApplicationBuilder(args);
builder.AddFactoryServerServices();

var app = builder.Build();

var client = await app.Services
    .GetRequiredService<IFactoryServerClientFactory>()
    .BuildFactoryServerClientAsync("192.168.1.10", 8080);

client.ServerStateChanged += (_, e) =>
{
    // e.ServerStateSnapshot.ServerState, .GameState, .Sessions, ...
};

client.ErrorOccurred += (_, e) =>
{
    // e.Exception / e.Error
};

using var cts = new CancellationTokenSource();
await client.InitializeClientAsync(cts.Token);
```

## UDP

The UDP entry point `IFactoryServerUdpClient`, obtained from a factory `IFactoryServerUdpClientFactory`, an example of use

```c#
var builder = Host.CreateApplicationBuilder(args);
builder.AddFactoryServerServices();

var app = builder.Build();

var factoryUdpClientFactory = app.Services.GetRequiredService<IFactoryServerUdpClientFactory>();

var factoryUdpClient = await factoryUdpClientFactory.BuildFactoryServerUdpClientAsync(host, port);

factoryUdpClient.ServerStateReceived += FactoryUdpClient_ServerStateReceived;
factoryUdpClient.ErrorOccurred += FactoryUdpClient_ErrorOccurred;

var listenerCTS = new CancellationTokenSource();

_ = factoryUdpClient.StartListeningAsync(listenerCTS.Token);

var pollTask = factoryUdpClient.StartPollingAsync(TimeSpan.FromMinutes(3));

while (!pollTask.IsCompleted)
{
}

listenerCTS.Cancel();

return 0;


private static void FactoryUdpClient_ErrorOccurred(object? sender, Exception e)
{
    ...
}

private static void FactoryUdpClient_ServerStateReceived(object? sender, FactoryServerStateUdpResponse e)
{
    var json = JsonSerializer.Serialize(e);
    ...

}
```

## HTTP

The HTTP entry point `IFactoryServerHttpClient`, obtained from a factory `IFactoryServerHttpClientFactory`:

```c#
var builder = Host.CreateApplicationBuilder(args);
builder.AddFactoryServerServices();

var app = builder.Build();

var httpClient = await app.Services
    .GetRequiredService<IFactoryServerHttpClientFactory>()
    .BuildFactoryServerHttpClientAsync("192.168.1.10", 8080);

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

## Configuration

Options are registered through `AddFactoryServerServices` with in-code defaults:

| Section | Option | Default |
| --- | --- | --- |
| `udpConfiguration` | `delayBetweenPolls` | `00:00:10` |
| | `messagesPerPoll` | `2` |
| | `timeoutRetriesBeforeStop` | `3` |
| | `protocolMagic` | `0xF6D5` |
| | `protocolVersion` | `1` |
| | `messageTermination` | `1` |
| `httpConfiguration` | `connectionTimeOut` | `00:00:30` |
| | `userAgentAppName` | `factoryServerApi` |
| | `userAgentAppVersion` | `0.3` |
| | `apiPath` | `api/v1/` |
| `sslConfiguration` | `serverCertificateValidationStrategy` | `NoValidation` |

Optionally, a `FactoryServerApi.settings.json` file in the current working directory can override
any of these keys (the file is read if present, never written by the library).

### SSL certificate validation

The default `NoValidation` strategy accepts **any** server certificate, including self-signed
ones, without checking them. This is convenient for the common self-signed setup of a home
dedicated server, but it exposes the connection (including the admin password and the
authentication tokens) to man-in-the-middle attacks. For untrusted networks set
`serverCertificateValidationStrategy` to `Custom` and register an
`IServerCertificateValidationStrategy` implementation in the service collection.

I'm new in this, and I made it as a hobby for managing remotely a server with my friends, don't be harsh.

Feel free to create issues for bugs and improvements, or fork.
