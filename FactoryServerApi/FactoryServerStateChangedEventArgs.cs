namespace FactoryServerApi;

public class FactoryServerStateChangedEventArgs : EventArgs
{

    public FactoryServerInfoSnapshot ServerStateSnapshot { get; }

    internal FactoryServerStateChangedEventArgs(FactoryServerInfo serverInfo)
    {
        ServerStateSnapshot = new FactoryServerInfoSnapshot(serverInfo);
    }
}