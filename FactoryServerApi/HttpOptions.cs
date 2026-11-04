
namespace FactoryServerApi;

public class HttpOptions
{
    private static readonly Version AssemblyMajorMinor = GetAssemblyMajorMinor();

    public string UserAgentAppName { get; } = "SatisfactoryApiClient";
    public Version UserAgentAppVersion { get; } = AssemblyMajorMinor;

    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public string ApiPath { get; set; } = "api/v1/";

    private static Version GetAssemblyMajorMinor()
    {
        Version? v = typeof(HttpOptions).Assembly.GetName().Version;
        return v is null ? new Version(0, 0) : new Version(v.Major, v.Minor);
    }
}