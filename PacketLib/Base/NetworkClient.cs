using System.Net;
using PacketLib.Packet;
using PacketLib.Util;

namespace PacketLib.Base;

/// <summary>
/// A client with transmitter.
/// </summary>
/// <typeparam name="T">The transmitter type.</typeparam>
public class NetworkClient<T> : IDisposable
    where T : TransmitterBase<T>
{
    /// <summary>
    /// Gets the ping (in milliseconds) to the server.
    /// </summary>
    public int Ping => Transmitter.Ping;
    
    /// <summary>
    /// The transmitter associated with this client.
    /// </summary>
    public readonly T Transmitter = Activator.CreateInstance<T>();
    
    /// <summary>
    /// The associated packet registry in this NetworkClient.
    /// </summary>
    public PacketRegistry Registry;

    /// <summary>
    /// The Guid associated with this NetworkClient, or null if the server hasn't provided one yet.
    /// </summary>
    public Guid? Guid;
    
    /// <summary>
    /// Event which gets triggered when this client finishes connecting and gets a packet from the server containing the Guid.
    /// </summary>
    public event EventHandler<Guid>? ClientConnected;
    internal void OnConnect() =>
        ClientConnected?.Invoke(this, Guid.Value);
    
    /// <summary>
    /// Event which gets triggered when this client is disconnected.
    /// </summary>
    public event EventHandler? ClientDisconnected;

    internal void OnDisconnect()
        => ClientDisconnected?.Invoke(this, EventArgs.Empty);
    
    /// <summary>
    /// Instantiate a new NetworkClient with a PacketRegistry.
    /// </summary>
    /// <param name="registry">The PacketRegistry to associate with the server.</param>
    public NetworkClient(PacketRegistry registry)
    {
        Registry = (PacketRegistry) registry.Clone();
    }

    /// <summary>
    /// Connect to a server.
    /// </summary>
    /// <param name="ipPort">The ip and port in format ip:port.</param>
    public void Connect(string ipPort)
    {
        Connect(HostUtil.ParseIpPort(ipPort));
    }

    /// <summary>
    /// Connect to a server.
    /// </summary>
    /// <param name="ip">The ip address to listen on.</param>
    /// <param name="port">The port to listen on.</param>
    public void Connect(string ip, int port)
    {
        Connect(HostUtil.ParseIpAddress(ip), port);
    }

    /// <summary>
    /// Connect to a server.
    /// </summary>
    /// <param name="ip">The ip address to listen on.</param>
    /// <param name="port">The port to listen on.</param>
    public void Connect(IPAddress ip, int port)
    {
        Connect(new IPEndPoint(ip, port));
    }

    /// <summary>
    /// Connect to a server.
    /// </summary>
    /// <param name="ipEndPoint">The IPEndPoint to listen on.</param>
    public void Connect(IPEndPoint ipEndPoint)
    {
        Transmitter.Connect(ipEndPoint, Registry);
    }

    /// <summary>
    /// Send a packet to the server.
    /// </summary>
    /// <param name="packet">The packet to send.</param>
    public void Send<T>(Packet<T> packet)
    {
        Transmitter.Send(stream => Registry.SerializePacket(packet, stream));
    }
    
    /// <summary>
    /// The interval at which pings should be sent.
    /// </summary>
    public TimeSpan PingInterval = TimeSpan.FromSeconds(1); // 1 ping per second default
    private DateTimeOffset? _lastPingSendTime = null;

    /// <summary>
    /// Process and read the current queue of packets.
    /// </summary>
    public void Poll()
    {
        var timeNow = DateTimeOffset.UtcNow;
        if (_lastPingSendTime == null)
        {
            _lastPingSendTime = timeNow;
            Transmitter.LastPingTime = timeNow; // Don't disconnect immediately.
        }
        if (Guid != null && timeNow > _lastPingSendTime + PingInterval)
        {
            _lastPingSendTime = timeNow;
            Send(Packet.Ping.CreateWithCurrent());
        }
        
        var result = Transmitter.Poll();
        if (result == null) return;
        
        foreach (var packet in result)
        {
            packet.ProcessClient(this);
        }

        if (Transmitter.ShouldQueueRemove()) // Check if the client has become invalid
        {
            OnDisconnect();
        }
    }

    public void Dispose()
    {
        Transmitter.Dispose();
    }
}