﻿using Microsoft.Extensions.DependencyInjection;
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

    public async Task<IFactoryServerUdpClient> BuildFactoryServerUdpClientAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        var checkHost = Uri.CheckHostName(host);
        IPAddress? iPAddress = null;
        if (checkHost == UriHostNameType.IPv4 || checkHost == UriHostNameType.IPv6)
        {
            _ = IPAddress.TryParse(host, out iPAddress);
        }
        else if(checkHost == UriHostNameType.Dns)
        {
            var uBuilder = new UriBuilder(null, host);
            var hostAddresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
            iPAddress = hostAddresses[0];
        }

        if (iPAddress is null)
            throw new ArgumentException("Invalid host");

        return BuildFactoryServerUdpService(new IPEndPoint(iPAddress, port));
    }
}
