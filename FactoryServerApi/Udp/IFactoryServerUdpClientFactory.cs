using System.Net;

namespace FactoryServerApi.Udp;

public interface IFactoryServerUdpClientFactory
{

    Task<IFactoryServerUdpClient> BuildFactoryServerUdpServiceAsync(string host, int port);

    IFactoryServerUdpClient BuildFactoryServerUdpService(IPAddress address, int port);

    IFactoryServerUdpClient BuildFactoryServerUdpService(IPEndPoint ipEndPoint);

}
