namespace PacketLib.Base;

/// <summary>
/// Transmitters transmit data.
/// A client has one transmitter for the server, a server has one transmitter per client.
/// </summary>
public interface ITransmitter : IDisposable
{
    
}