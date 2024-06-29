using System.Net;

namespace PacketLib.Base;

/// <summary>
/// A client reference from the server side.
/// </summary>
/// <typeparam name="T">The transmitter type.</typeparam>
public class ClientRef<T> : IDisposable
    where T : ITransmitter
{
    public Guid Guid;
    public IPEndPoint IpEndPoint;
    public T Transmitter;

    public ClientRef(Guid guid, IPEndPoint ipEndPoint, T transmitter)
    {
        Guid = guid;
        IpEndPoint = ipEndPoint;
        Transmitter = transmitter;
    }

    public void Dispose()
    {
        Transmitter.Dispose();
    }
}