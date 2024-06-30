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
    /// <summary>
    /// Gets the ping (in milliseconds) to the server.
    /// </summary>
    public int Ping => Transmitter.Ping;
    
    /// <summary>
    /// The Guid associated with this ClientRef.
    /// </summary>
    public Guid Guid;
    
    /// <summary>
    /// The IPEndPoint associated with this ClientRef.
    /// </summary>
    public IPEndPoint IpEndPoint;
    
    /// <summary>
    /// The transmitter associated with this ClientRef.
    /// </summary>
    public readonly T Transmitter;

    /// <summary>
    /// The NetworkServer associated with this ClientRef.
    /// </summary>
    public NetworkServer<T> Server;

    internal ClientRef(Guid guid, IPEndPoint ipEndPoint, T transmitter, NetworkServer<T> server)
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

    /// <summary>
    /// Disconnect this client.
    /// </summary>
    public void Disconnect()
    {
        Transmitter.Disconnect();
    }
    
    /// <summary>
    /// Send a packet to this client.
    /// </summary>
    /// <param name="packet">The packet to send.</param>
    public void Send<T>(Packet<T> packet)
    {
        Transmitter.Send(stream => Server.Registry.SerializePacket(packet, stream));
    }

    /// <summary>
    /// Process and read the current queue of packets.
    /// </summary>
    public void Poll()
    {
        var result = Transmitter.Poll();
        if (result == null) return;
        
        foreach (var packet in result)
        {
            packet.ProcessServer(Server, this);
        }
    }

    /// <summary>
    /// Check if this transmitter should be removed (ClientRef only).
    /// </summary>
    /// <returns>true if the transmitter should be removed, otherwise false.</returns>
    public bool ShouldQueueRemove() => Transmitter.ShouldQueueRemove();
}