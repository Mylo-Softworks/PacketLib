using System.Net;
using PacketLib.Packet;

namespace PacketLib.Base;

public enum TransmitterState
{
    Inactive,
    Client,
    Server
}

/// <summary>
/// Transmitters transmit data.
/// A client has one transmitter for the server, a server has one transmitter per client.
/// </summary>
public abstract class TransmitterBase<Self> : IDisposable
    where Self : TransmitterBase<Self>
{
    public TransmitterState State = TransmitterState.Inactive;
    
    public delegate void TransmitterStateChangedHandler(TransmitterBase<Self> sender, IPEndPoint endPoint, TransmitterBase<Self> newTransmitter);

    public event TransmitterStateChangedHandler? NewServerConnection;

    protected void OnNewServerConnection(IPEndPoint endPoint, TransmitterBase<Self> newTransmitter)
    {
        NewServerConnection?.Invoke(this, endPoint, newTransmitter);
    }
    
    public event EventHandler? NewClientConnection;

    protected void OnNewClientConnection()
    {
        NewClientConnection?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Init with a preexisting transfer protocol. Used to init client refs on the server.
    /// </summary>
    /// <param name="transfer"></param>
    public void InitClientRef(object transfer)
    {
        if (State != TransmitterState.Inactive) throw new InvalidOperationException("Transmitter is already active.");
        State = TransmitterState.Client;
        InitClientRefImpl(transfer);
    }
    
    /// <summary>
    /// Connect to an IPEndPoint as a client.
    /// </summary>
    /// <param name="host"></param>
    public void Connect(IPEndPoint host)
    {
        if (State != TransmitterState.Inactive) throw new InvalidOperationException("Transmitter is already active.");
        State = TransmitterState.Client;
        ConnectImpl(host);
    }

    /// <summary>
    /// Start a server, this transmitter will be used as a server now.
    /// </summary>
    /// <param name="host"></param>
    public void Host(IPEndPoint host)
    {
        if (State != TransmitterState.Inactive) throw new InvalidOperationException("Transmitter is already active.");
        State = TransmitterState.Server;
        HostImpl(host);
    }

    public void Disconnect(PacketRegistry registry)
    {
        if (State == TransmitterState.Inactive) throw new InvalidOperationException("Transmitter is not active.");
        State = TransmitterState.Inactive;
        SendImpl(stream => registry.SerializePacket(new Disconnect(), stream)); // Send disconnect packet
        DisconnectImpl();
    }

    public void Send(Action<Stream> streamWrite)
    {
        if (State == TransmitterState.Inactive) throw new InvalidOperationException("Transmitter is not active.");
        
        SendImpl(streamWrite);
    }

    public List<dynamic>? Poll(PacketRegistry registry)
    {
        if (State == TransmitterState.Inactive) throw new InvalidOperationException("Transmitter is not active.");

        return PollImpl(registry);
    }
    
    // Abstract impls

    /// <summary>
    /// Init with a preexisting transfer protocol. Used to init client refs on the server.
    /// </summary>
    /// <param name="transfer"></param>
    protected abstract void InitClientRefImpl(object transfer);
    
    /// <summary>
    /// Connect to an IPEndPoint as a client.
    /// </summary>
    /// <param name="host"></param>
    protected abstract void ConnectImpl(IPEndPoint host);
    
    /// <summary>
    /// Start a server, this transmitter will be used as a server now.
    /// </summary>
    /// <param name="host"></param>
    protected abstract void HostImpl(IPEndPoint host);
    
    protected abstract void DisconnectImpl();
    
    protected abstract void SendImpl(Action<Stream> streamWrite);
    
    protected abstract List<dynamic>? PollImpl(PacketRegistry registry);
    
    // Public abstract functions

    public abstract bool IsConnected();
    public abstract bool IsConnecting();

    public abstract bool ShouldQueueRemove();
    
    // Inherited functions

    public abstract void Dispose();
}