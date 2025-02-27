﻿using System.Net;
using PacketLib.Packet;

namespace PacketLib.Base;

/// <summary>
/// An enum indicating the current state and function of a transmitter.
/// </summary>
public enum TransmitterState
{
    /// <summary>
    /// When the transmitter is not used yet.
    /// </summary>
    Inactive,
    /// <summary>
    /// When the transmitter is part of a NetworkClient or ClientRef.
    /// </summary>
    Client,
    /// <summary>
    /// When the transmitter is part of a NetworkServer.
    /// </summary>
    Server
}

/// <summary>
/// Transmitters transmit data.
/// A client has one transmitter for the server, a server has one transmitter per client.
/// </summary>
public abstract class TransmitterBase<Self> : IDisposable
    where Self : TransmitterBase<Self>
{
    /// <summary>
    /// If no ping packets have been received for this duration, treat as disconnected.
    /// </summary>
    public TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// The time the last ping was received at.
    /// </summary>
    public DateTimeOffset? LastPingTime = null;
    
    /// <summary>
    /// The last known ping in milliseconds. -1 if unknown.
    /// </summary>
    public int Ping = -1;

    public PacketRegistry Registry;
    
    /// <summary>
    /// The current state of the transmitter.
    /// </summary>
    public TransmitterState State { get; private set; } = TransmitterState.Inactive;

    public delegate void TransmitterStateChangedHandler(TransmitterBase<Self> sender, EndPoint? endPoint, TransmitterBase<Self> newTransmitter);

    /// <summary>
    /// Event which gets triggered if this is a server transmitter and a new client just connected.
    /// </summary>
    public event TransmitterStateChangedHandler? NewServerConnection;

    protected void OnNewServerConnection(EndPoint? endPoint, TransmitterBase<Self> newTransmitter)
    {
        NewServerConnection?.Invoke(this, endPoint, newTransmitter);
    }
    
    /// <summary>
    /// Event which gets triggered if this is a client transmitter and a connections has been made (But no data has been sent yet).
    /// </summary>
    public event EventHandler? NewClientConnection;

    protected void OnNewClientConnection()
    {
        NewClientConnection?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Init with a preexisting transfer protocol. Used to init client refs on the server.
    /// </summary>
    /// <param name="transfer"></param>
    /// <param name="registry"></param>
    public void InitClientRef(object transfer, PacketRegistry registry)
    {
        if (State != TransmitterState.Inactive) throw new InvalidOperationException("Transmitter is already active.");
        State = TransmitterState.Client;
        Registry = (PacketRegistry) registry.Clone();
        InitClientRefImpl(transfer);
    }
    
    /// <summary>
    /// Connect to an EndPoint as a client.
    /// </summary>
    /// <param name="host"></param>
    public void Connect(EndPoint host, PacketRegistry registry)
    {
        if (State != TransmitterState.Inactive) throw new InvalidOperationException("Transmitter is already active.");
        State = TransmitterState.Client;
        Registry = (PacketRegistry) registry.Clone();
        ConnectImpl(host);
    }

    /// <summary>
    /// Start a server, this transmitter will be used as a server now.
    /// </summary>
    /// <param name="host"></param>
    public void Host(EndPoint host, PacketRegistry registry)
    {
        if (State != TransmitterState.Inactive) throw new InvalidOperationException("Transmitter is already active.");
        State = TransmitterState.Server;
        Registry = (PacketRegistry) registry.Clone();
        HostImpl(host);
    }

    /// <summary>
    /// Disconnect the transmitter, most transmitters will become invalid after this.
    /// </summary>
    /// <exception cref="InvalidOperationException">If the transmitter isn't used yet.</exception>
    public void Disconnect()
    {
        if (State == TransmitterState.Inactive) throw new InvalidOperationException("Transmitter is not active.");
        State = TransmitterState.Inactive;
        SendImpl(stream => Registry.SerializePacket(new Disconnect(), stream)); // Send disconnect packet
        DisconnectImpl();
    }

    /// <summary>
    /// Send data over this transmitter.
    /// </summary>
    /// <param name="streamWrite">An action which gives a stream to write the packet to.</param>
    /// <exception cref="InvalidOperationException">If the transmitter isn't used yet.</exception>
    public void Send(Action<Stream> streamWrite)
    {
        if (State == TransmitterState.Inactive) throw new InvalidOperationException("Transmitter is not active.");
        
        SendImpl(streamWrite);
    }

    /// <summary>
    /// Get the current list of queued packets, or null if no packets are queued.
    /// </summary>
    /// <returns>A list of packets queued, or null if no packets are queued.</returns>
    /// <exception cref="InvalidOperationException">If the transmitter isn't used yet.</exception>
    public List<dynamic>? Poll()
    {
        if (State == TransmitterState.Inactive) throw new InvalidOperationException("Transmitter is not active.");

        return PollImpl();
    }

    /// <summary>
    /// Check if this transmitter should be removed (ClientRef only).
    /// </summary>
    /// <returns>true if the transmitter should be removed, otherwise false.</returns>
    public bool ShouldQueueRemove()
    {
        return CheckTimeout() || ShouldQueueRemoveImpl();
    }
    
    /// <summary>
    /// Check if client has timed out.
    /// </summary>
    /// <returns>true if client timed out, false if client has not timed out.</returns>
    public bool CheckTimeout()
    {
        if (LastPingTime == null) return false; // Not initialized yet
        
        return (DateTimeOffset.UtcNow - LastPingTime) > ConnectionTimeout;
    }
    
    // Abstract impls

    /// <summary>
    /// Init with a preexisting transfer protocol. Used to init client refs on the server.
    /// </summary>
    /// <param name="transfer">The client object, in case of TcpTransmitter, the type should be TcpClient.</param>
    protected abstract void InitClientRefImpl(object transfer);
    
    /// <summary>
    /// Connect to an EndPoint as a client.
    /// </summary>
    /// <param name="host">The EndPoint to connect to.</param>
    protected abstract void ConnectImpl(EndPoint host);
    
    /// <summary>
    /// Start a server, this transmitter will be used as a server now.
    /// </summary>
    /// <param name="host">The EndPoint to host on.</param>
    protected abstract void HostImpl(EndPoint host);
    
    /// <summary>
    /// Disconnect this transmitter.
    /// </summary>
    protected abstract void DisconnectImpl();
    
    /// <summary>
    /// Send the data written to the stream.
    /// </summary>
    /// <param name="streamWrite">An action which writes to the provided stream.</param>
    protected abstract void SendImpl(Action<Stream> streamWrite);
    
    /// <summary>
    /// Perform the polling, should be done with registry.ReadPacketDataUntilThisPoint(stream);
    /// </summary>
    /// <returns>A list of packets queued, or null if no packets are queued.</returns>
    protected abstract List<dynamic>? PollImpl();
    
    // Public abstract functions

    /// <summary>
    /// Check if this transmitter is connected.
    /// </summary>
    /// <returns>true if the transmitter is connected, false if it isn't.</returns>
    public abstract bool IsConnected();
    
    /// <summary>
    /// Check if this transmitter is still busy connecting.
    /// </summary>
    /// <returns>true if the transmitter is still connecting, false if it hasn't started, or is done connecting.</returns>
    public abstract bool IsConnecting();

    /// <summary>
    /// Check if this transmitter should be removed (ClientRef only).
    /// </summary>
    /// <returns>true if the transmitter should be removed, otherwise false.</returns>
    public abstract bool ShouldQueueRemoveImpl();
    
    // Inherited functions

    public abstract void Dispose();
}