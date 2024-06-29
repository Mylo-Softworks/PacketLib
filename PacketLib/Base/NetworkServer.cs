using System.Net;
using PacketLib.Packet;
using PacketLib.Util;

namespace PacketLib.Base;

public class NetworkServer<T> : IDisposable
    where T : ITransmitter
{
    public Dictionary<Guid, ClientRef<T>> Clients = new ();

    public PacketRegistry Registry;

    public NetworkServer(PacketRegistry registry)
    {
        Registry = registry;
    }

    public void Start(int port, bool shareLocal = false)
    {
        Start(shareLocal ? "0.0.0.0" : "127.0.0.1", port);
    }

    public void Start(string ipPort)
    {
        Start(HostUtil.ParseIpPort(ipPort));
    }

    public void Start(string ip, int port)
    {
        Start(HostUtil.ParseIpAddress(ip), port);
    }

    public void Start(IPAddress ip, int port)
    {
        Start(new IPEndPoint(ip, port));
    }

    public void Start(IPEndPoint ipEndPoint)
    {
        
    }

    public void Dispose()
    {
        foreach (var client in Clients.Values)
        {
            client.Dispose();
        }
        Clients.Clear();
    }
}