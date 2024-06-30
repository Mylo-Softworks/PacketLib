using System.Net;
using PacketLib.Packet;
using PacketLib.Util;
using SerializeLib;

namespace PacketLib.Base;

/// <summary>
/// A server with transmitter.
/// </summary>
/// <typeparam name="T">The transmitter type.</typeparam>
public class NetworkServer<T> : IDisposable
    where T : TransmitterBase<T>
{
    /// <summary>
    /// Gets the ping (in milliseconds) to the server.
    /// </summary>
    public int Ping => ServerTransmitter.Ping;
    
    /// <summary>
    /// A dictionary containing the clients and their ids.
    /// </summary>
    public Dictionary<Guid, ClientRef<T>> Clients = new ();
    
    /// <summary>
    /// The transmitter associated with this server.
    /// </summary>
    public readonly T ServerTransmitter = Activator.CreateInstance<T>();

    /// <summary>
    /// Event which gets triggered when a new client has connected.
    /// </summary>
    public event EventHandler<ClientRef<T>>? ClientConnected;
    
    /// <summary>
    /// Event which gets triggered when a client has disconnected.
    /// </summary>
    public event EventHandler<ClientRef<T>>? ClientDisconnected;
    
    internal void OnDisconnect(ClientRef<T> clientRef)
        => ClientDisconnected?.Invoke(this, clientRef);

    /// <summary>
    /// The associated packet registry in this NetworkServer.
    /// </summary>
    public PacketRegistry Registry;

    /// <summary>
    /// Instantiate a new NetworkServer with a PacketRegistry.
    /// </summary>
    /// <param name="registry">The PacketRegistry to associate with the server.</param>
    public NetworkServer(PacketRegistry registry)
    {
        Registry = (PacketRegistry) registry.Clone();
    }

    /// <summary>
    /// Start the server.
    /// </summary>
    /// <param name="port">The port to listen on.</param>
    /// <param name="shareLocal">If the server should be shared over the local network.</param>
    public void Start(int port, bool shareLocal = true)
    {
        Start(shareLocal ? "0.0.0.0" : "127.0.0.1", port);
    }

    /// <summary>
    /// Start the server.
    /// </summary>
    /// <param name="ipPort">The ip and port in format ip:port.</param>
    public void Start(string ipPort)
    {
        Start(HostUtil.ParseIpPort(ipPort));
    }

    /// <summary>
    /// Start the server.
    /// </summary>
    /// <param name="ip">The ip address to listen on.</param>
    /// <param name="port">The port to listen on.</param>
    public void Start(string ip, int port)
    {
        Start(HostUtil.ParseIpAddress(ip), port);
    }

    /// <summary>
    /// Start the server.
    /// </summary>
    /// <param name="ip">The ip address to listen on.</param>
    /// <param name="port">The port to listen on.</param>
    public void Start(IPAddress ip, int port)
    {
        Start(new IPEndPoint(ip, port));
    }

    /// <summary>
    /// Start the server.
    /// </summary>
    /// <param name="ipEndPoint">The IPEndPoint to listen on.</param>
    public void Start(IPEndPoint ipEndPoint)
    {
        ServerTransmitter.NewServerConnection += (sender, point, transmitter) => 
        {
            var clientGuid = Guid.NewGuid();
            
            var clientObj = new ClientRef<T>(clientGuid, point, (transmitter as T)!, this);
            Clients[clientGuid] = clientObj;
            
            clientObj.Send(new Connect() {Payload = new GuidPayload(){Guid = clientGuid}});
            
            ClientConnected?.Invoke(this, clientObj); // Trigger connection event
        };
        ServerTransmitter.Host(ipEndPoint, Registry);
    }

    /// <summary>
    /// Send a packet to all clients.
    /// </summary>
    /// <param name="packet">The packet to send.</param>
    public void SendToAll<T>(Packet<T> packet)
    {
        foreach (var client in Clients.Values)
        {
            client.Send(packet);
        }
    }

    /// <summary>
    /// Send a packet to a single client.
    /// </summary>
    /// <param name="packet">The packet to send.</param>
    /// <param name="clientId">The client Id to send the packet to.</param>
    /// <returns>True if the client exists and packet was sent, false if the client doesn't exist.</returns>
    public bool SendToClient<T>(Packet<T> packet, Guid clientId)
    {
        var client = Clients.GetValueOrDefault(clientId);
        if (client == null) return false;
        client.Send(packet);
        return true;
    }
    
    /// <summary>
    /// Process and read the current queue of packets.
    /// </summary>
    public void Poll()
    {
        var removeQueue = new List<Guid>();
        foreach (var client in Clients.Values)
        {
            client.Poll();
            if (client.ShouldQueueRemove()) removeQueue.Add(client.Guid);
        }
        
        RemoveClients(removeQueue);
    }

    private void RemoveClients(List<Guid> clientGuids)
    {
        foreach (var guid in clientGuids)
        {
            Clients.GetValueOrDefault(guid)?.Dispose();
            Clients.Remove(guid);
        }
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