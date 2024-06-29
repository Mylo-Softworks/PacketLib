using System.Net;
using PacketLib.Packet;
using PacketLib.Util;

namespace PacketLib.Base;

public class NetworkServer<T> : IDisposable
    where T : TransmitterBase<T>
{
    public Dictionary<Guid, ClientRef<T>> Clients = new ();
    public readonly T ServerTransmitter = Activator.CreateInstance<T>();

    public event EventHandler<ClientRef<T>>? ClientConnected;
    public event EventHandler<ClientRef<T>>? ClientDisconnected;
    
    internal void OnDisconnect(ClientRef<T> clientRef)
        => ClientDisconnected?.Invoke(this, clientRef);

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
        ServerTransmitter.NewServerConnection += (sender, point, transmitter) => 
        {
            var clientGuid = Guid.NewGuid();
            
            var clientObj = new ClientRef<T>(clientGuid, point, (transmitter as T)!, this);
            Clients[clientGuid] = clientObj;
            
            clientObj.Send(new Connect() {Payload = new GuidPayload(){Guid = clientGuid}});
            
            ClientConnected?.Invoke(this, clientObj); // Trigger connection event
        };
        ServerTransmitter.Host(ipEndPoint);
    }

    public void SendToAll<T>(Packet<T> packet)
    {
        foreach (var client in Clients.Values)
        {
            client.Send(packet);
        }
    }

    public bool SendToClient<T>(Packet<T> packet, Guid clientId)
    {
        var client = Clients.GetValueOrDefault(clientId);
        if (client == null) return false;
        client.Send(packet);
        return true;
    }
    
    public void Poll()
    {
        var removeQueue = new List<Guid>();
        foreach (var client in Clients.Values)
        {
            client.Poll();
            if (client.ShouldQueueRemove()) removeQueue.Add(client.Guid);
        }
        
        foreach (var guid in removeQueue)
        {
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