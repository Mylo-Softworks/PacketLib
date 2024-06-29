using System.Net;
using PacketLib.Util;

namespace PacketLib.Base;

public class NetworkServer
{
    public List<ClientRef> Clients = new ();

    public void Start(int port, bool shareLocal = false) // 1
    {
        Start(shareLocal ? "0.0.0.0" : "127.0.0.1", port); // 3
    }

    public void Start(string ipPort) // 2
    {
        Start(HostUtil.ParseIpPort(ipPort)); // 4
    }

    public void Start(string ip, int port) // 3
    {
        Start(HostUtil.ParseIpAddress(ip), port); // 4
    }

    public void Start(IPAddress ip, int port) // 4
    {
        Start(new IPEndPoint(ip, port)); // 5
    }

    public void Start(IPEndPoint ipEndPoint) // 5
    {
        
    }
}