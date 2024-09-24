namespace FactoryServerApi;

public class HttpOptions
{
    public TimeSpan ConnectionTimeout { get; init; }
    public string UserAgentAppName { get; init; } = default!;
    public Version? UserAgentAppVersion { get; init; }
    public string ApiPath { get; init; } = default!;
}
