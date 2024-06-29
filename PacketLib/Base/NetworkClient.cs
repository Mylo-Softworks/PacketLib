﻿using System.Net;
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
        Registry = registry;
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
        Transmitter.Connect(ipEndPoint);
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
    /// Process and read the current queue of packets.
    /// </summary>
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