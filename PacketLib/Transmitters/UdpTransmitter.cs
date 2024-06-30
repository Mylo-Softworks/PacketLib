using System.Net;
using System.Net.Sockets;
using PacketLib.Base;
using PacketLib.Packet;

namespace PacketLib.Transmitters;

/// <summary>
/// A transmitter for UDP which uses UdpClient for clients, and UdpClient for servers.
/// </summary>
public class UdpTransmitter : TransmitterBase<UdpTransmitter>
{
    private UdpClient? _udpClient = null;
    private IPEndPoint? _localIpEndpoint = null;
    private IPEndPoint? _remoteIpEndpoint = null;
    
    private Dictionary<IPEndPoint, UdpTransmitter> _createdClients = new ();

    private UdpTransmitter AccessClient(IPEndPoint remoteEndPoint)
    {
        if (_createdClients.TryGetValue(remoteEndPoint, out var client))
            return client;
        
        // If no client is found, add one.
        var newTransmitter = new UdpTransmitter();
        newTransmitter.InitClientRef((remoteEndPoint, _udpClient), Registry);
        _createdClients.Add(remoteEndPoint, newTransmitter);
        OnNewServerConnection(remoteEndPoint, newTransmitter); // Signal to the server that a client has connected.
        return newTransmitter;
    }
    
    protected override void InitClientRefImpl(object transfer)
    {
        (_remoteIpEndpoint, _udpClient) = (ValueTuple<IPEndPoint, UdpClient>)transfer;
    }

    protected override void ConnectImpl(IPEndPoint host)
    {
        _udpClient = new UdpClient(0);
        _localIpEndpoint = _udpClient.Client.LocalEndPoint as IPEndPoint;
        _remoteIpEndpoint = host;

        StartReceive();
        Send(stream => Registry.SerializePacket(new Connect(), stream));
    }

    protected override void HostImpl(IPEndPoint host)
    {
        _udpClient = new UdpClient(host);
        _localIpEndpoint = _udpClient.Client.LocalEndPoint as IPEndPoint;
        
        StartReceive();
    }

    protected override void DisconnectImpl()
    {
        Dispose();
    }

    protected override void SendImpl(Action<Stream> streamWrite)
    {
        var stream = new MemoryStream();
        streamWrite(stream);
        var bytes = stream.ToArray();

        _udpClient.Send(bytes, bytes.Length, _remoteIpEndpoint);
    }

    /// <summary>
    /// A queue of packets received, to be processed on poll.
    /// </summary>
    // public MemoryStream PacketsReadQueue = new MemoryStream();
    public List<dynamic> PacketsReadQueue = new ();

    protected override List<dynamic>? PollImpl()
    {
        if (PacketsReadQueue.Count == 0) return null;
        
        var copy = PacketsReadQueue;
        PacketsReadQueue = new List<dynamic>(); // Instead of using Clear(), which would clear the returned queue.
        
        return copy;
    }

    private void ReceiveLoop(IAsyncResult result)
    {
        var endPoint = new IPEndPoint(IPAddress.Any, 0);
        var data = _udpClient?.EndReceive(result, ref endPoint);
        if (data != null && endPoint != null)
        {
            var parsedPacket = Registry.ReadPacketDataUntilThisPoint(new MemoryStream(data));
            if (parsedPacket != null)
            {
                if (State == TransmitterState.Server)
                {
                    var client = AccessClient(endPoint);
                    client.PacketsReadQueue.AddRange(parsedPacket);
                }
                else
                {
                    PacketsReadQueue.AddRange(parsedPacket);
                }
            }
        }
        StartReceive();
    }
    
    private void StartReceive()
    {
        _udpClient?.BeginReceive(ReceiveLoop, null);
    }

    public override bool IsConnected()
    {
        return true;
    }

    public override bool IsConnecting()
    {
        return false;
    }

    public override bool ShouldQueueRemoveImpl()
    {
        return _udpClient == null;
    }

    public override void Dispose()
    {
        _udpClient?.Close();
        _udpClient?.Dispose();
        _udpClient = null;
    }
}