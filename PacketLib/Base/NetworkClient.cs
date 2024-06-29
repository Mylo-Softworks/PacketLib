using System.Net;
using PacketLib.Packet;
using PacketLib.Util;

namespace PacketLib.Base;

public class NetworkClient<T> : IDisposable
    where T : TransmitterBase<T>
{
    public readonly T Transmitter = Activator.CreateInstance<T>();
    
    public PacketRegistry Registry;

    public Guid? Guid;
    
    public event EventHandler? ClientConnected;
    public event EventHandler? ClientDisconnected;

    internal void OnDisconnect()
        => ClientDisconnected?.Invoke(this, EventArgs.Empty);
    

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

    public void Send<T>(Packet<T> packet)
    {
        Transmitter.Send(stream => Registry.SerializePacket(packet, stream));
    }

    public void Poll()
    {
        var result = Transmitter.Poll(Registry);
        if (result == null) return;
        
        foreach (var packet in result)
        {
            packet.ProcessClient(this);
        }
    }

    public void Dispose()
    {
        Transmitter.Dispose();
    }
}