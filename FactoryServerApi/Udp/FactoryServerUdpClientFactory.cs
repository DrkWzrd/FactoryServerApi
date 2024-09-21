using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;

namespace FactoryServerApi.Udp;

internal class FactoryServerUdpClientFactory : IFactoryServerUdpClientFactory
{
    private readonly IServiceProvider _sProvider;

    public FactoryServerUdpClientFactory(IServiceProvider sProvider)
    {
        _sProvider = sProvider;
    }

    public IFactoryServerUdpClient BuildFactoryServerUdpService(IPAddress address, int port)
    {
        return BuildFactoryServerUdpService(new IPEndPoint(address, port));
    }

    public IFactoryServerUdpClient BuildFactoryServerUdpService(IPEndPoint serverEndPoint)
    {
        var udpOptions = _sProvider.GetRequiredService<IOptions<UdpOptions>>();
        return new FactoryServerUdpClient(serverEndPoint, _sProvider.GetRequiredKeyedService<TimeProvider>("factoryServerLocalSystemTimeProvider"), udpOptions);
    }

    public async Task<IFactoryServerUdpClient> BuildFactoryServerUdpClientAsync(string url, int port, CancellationToken cancellationToken = default)
    {
        var uri = new Uri(url);

        if (!IPAddress.TryParse(uri.Host, out var ipAddress))
        {
            var hostAddresses = await Dns.GetHostAddressesAsync(uri.Host, cancellationToken);
            ipAddress = hostAddresses[0];
        }
        return BuildFactoryServerUdpService(new IPEndPoint(ipAddress, port));
    }
}
