### Simple Satisfactory Server Api for .net

An implementation of the documentation file of the Satisfactory dedicated server API, both UDP and HTTP.

Is integrated with generic host and fully async.

## UDP

The UDP entry point `IFactoryServerUdpClient`, obtained from a factory `IFactoryServerUdpClientFactory`, an example of use

```c#
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddFactoryServerServices();

var app = builder.Build();

var factoryUdpClientFactory = app.Services.GetRequiredService<IFactoryServerUdpClientFactory>();

var factoryUdpClient = await factoryUdpClientFactory.BuildFactoryServerUdpServiceAsync("urlOrIPAddress", port);

factoryUdpClient.ServerStateReceived += FactoryUdpClient_ServerStateReceived;
factoryUdpClient.ErrorOccurred += FactoryUdpClient_ErrorOccurred;

var listenerCTS = new CancellationTokenSource();

_ = factoryUdpClient.StartListeningAsync(listenerCTS.Token);

var pollTask = factoryUdpClient.PollServerStateAsync(TimeSpan.FromMinutes(3), TimeSpan.FromSeconds(10), false, 1, null);


while(!pollTask.IsCompleted){

}

listenerCTS.Cancel();

return 0;


private static void FactoryUdpClient_ErrorOccurred(object? sender, Exception e)
{
    ...
}

private static void FactoryUdpClient_ServerStateReceived(object? sender, FactoryServerStateResponse e)
{
    var json = JsonSerializer.Serialize(e);
    ...

}
```

## HTTP

The HTTP entry point `IFactoryServerHttpService`, an example of use

```c#

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddFactoryServerServices();

var app = builder.Build();

var factoryServer = app.Services.GetRequiredService<IFactoryServerHttpService>();

//Here in connection info you can set the AuthenticationToken provided for the login functions or third party tokens provided by server
var connectionInfo = new FactoryServerConnectionInfo("https_url", port);

var result = await factoryServer.HealthCheckAsync(null, connectionInfo);

string? json = null;
if (result.Result is not null)
{
    json = JsonSerializer.Serialize(result.Result);
}
else if (result.Error is not null)
{
    json = JsonSerializer.Serialize(result.Error);
}

```

I'm new in this, and I made it as a hobby for managing remotely a server with my friends, don't be harsh.

Feel free to create issues for bugs and improvements, or fork.
