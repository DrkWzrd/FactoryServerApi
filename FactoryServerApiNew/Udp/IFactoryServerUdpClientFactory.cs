using System.Net;

namespace FactoryServerApiNew.Udp;

public interface IFactoryServerUdpClientFactory
{

    Task<IFactoryServerUdpClient> BuildFactoryServerUdpClientAsync(string host, int port, CancellationToken cancellationToken = default);

    IFactoryServerUdpClient BuildFactoryServerUdpService(IPAddress address, int port);

    IFactoryServerUdpClient BuildFactoryServerUdpService(IPEndPoint ipEndPoint);

}
