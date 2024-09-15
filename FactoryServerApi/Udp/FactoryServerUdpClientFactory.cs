using Microsoft.Extensions.DependencyInjection;
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
        return new FactoryServerUdpClient(serverEndPoint, _sProvider.GetRequiredKeyedService<TimeProvider>("factoryServerLocalSystemTimeProvider"));
    }

    public async Task<IFactoryServerUdpClient> BuildFactoryServerUdpServiceAsync(string host, int port)
    {
        if (!IPAddress.TryParse(host, out var ipAddress))
        {
            var hostEntry = await Dns.GetHostEntryAsync(host);
            ipAddress = hostEntry.AddressList[0];
        }
        return BuildFactoryServerUdpService(new IPEndPoint(ipAddress, port));
    }
}
