namespace FactoryServerApi;

public class UdpOptions
{

    public TimeSpan DelayBetweenPolls { get; init; }

    public int MessagesPerPoll { get; init; }

    public int TimeoutRetriesBeforeStop { get; init; }
}

public class HttpOptions
{
    public const string DefaultApiPath = "api/v1/";
    private readonly string _apiPath = DefaultApiPath;

    public TimeSpan ConnectionTimeout { get; init; }
    public string UserAgentAppName { get; init; } = default!;
    public Version? UserAgentAppVersion { get; init; }
    public string ApiPath
    {
        get => _apiPath;
        init => _apiPath = value;
    }
}
