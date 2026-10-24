
namespace FactoryServerApiNew;

public class HttpOptions
{
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public string UserAgentAppName { get; set; } = "FactoryServerApiNew";
    public Version? UserAgentAppVersion { get; set; } = new(0, 3);
    public string ApiPath { get; set; } = "api/v1/";
}
