using System.Net;
using PacketLib.Packet;

namespace PacketLib.Base;

/// <summary>
/// A client reference from the server side.
/// </summary>
/// <typeparam name="T">The transmitter type.</typeparam>
public class ClientRef<T> : IDisposable
    where T : TransmitterBase<T>
{
    public Guid Guid;
    public IPEndPoint IpEndPoint;
    public readonly T Transmitter;

    public NetworkServer<T> Server;

    public ClientRef(Guid guid, IPEndPoint ipEndPoint, T transmitter, NetworkServer<T> server)
    {
        Guid = guid;
        IpEndPoint = ipEndPoint;
        Transmitter = transmitter;
        Server = server;
    }

    public void Dispose()
    {
        Transmitter.Dispose();
    }

    public void Disconnect()
    {
        Transmitter.Send(stream => Server.Registry.SerializePacket(new Disconnect(), stream));
    }
    
    public void Send<T>(Packet<T> packet)
    {
        Transmitter.Send(stream => Server.Registry.SerializePacket(packet, stream));
    }

    public void Poll()
    {
        var result = Transmitter.Poll(Server.Registry);
        if (result == null) return;
        
        foreach (var packet in result)
        {
            packet.ProcessServer(this);
        }
    }

    public bool ShouldQueueRemove() => Transmitter.ShouldQueueRemove();
}