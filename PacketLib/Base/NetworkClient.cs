using System.Net;
using PacketLib.Packet;
using PacketLib.Util;

namespace PacketLib.Base;

public class NetworkClient<T> where T : ITransmitter
{
    private T _transmitter = Activator.CreateInstance<T>();
    
    public PacketRegistry Registry;

    public NetworkClient(PacketRegistry registry)
    {
        Registry = registry;
    }

    public void Connect(string ipPort)
    {
        Connect(HostUtil.ParseIpPort(ipPort));
    }

    public void Connect(string ip, int port)
    {
        Connect(HostUtil.ParseIpAddress(ip), port);
    }

    public void Connect(IPAddress ip, int port)
    {
        Connect(new IPEndPoint(ip, port));
    }

    public void Connect(IPEndPoint ipEndPoint)
    {
        
    }
}