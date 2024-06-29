using System.Net;
using PacketLib.Packet;
using PacketLib.Util;

namespace PacketLib.Base;

using Packet = Packet<object>;

public class NetworkClient<T> where T : TransmitterBase<T>
{
    public readonly T Transmitter = Activator.CreateInstance<T>();
    
    public PacketRegistry Registry;
    
    public event EventHandler? ClientConnected;

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
        Transmitter.NewClientConnection += (sender, args) =>
        {
            ClientConnected?.Invoke(this, args);
        };
        Transmitter.Connect(ipEndPoint);
    }

    public void Send(Packet packet)
    {
        Transmitter.Send(stream => Registry.SerializePacket(packet, stream));
    }
}