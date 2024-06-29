using PacketLib.Base;
using SerializeLib;
using SerializeLib.Attributes;
using SerializeLib.Interfaces;

namespace PacketLib.Packet;

/// <summary>
/// The base class for packets, should be inherited. Data is accessible through Payload.
/// </summary>
/// <typeparam name="T">The type used for storing the packet's data contents. Must be serializable with SerializeLib.</typeparam>
public abstract class Packet<T> : ISerializableClass<Packet<T>>
{
    /// <summary>
    /// The data in this packet, must be serializable.
    /// </summary>
    public T Payload = Activator.CreateInstance<T>();
    
    public void Serialize(Stream s)
    {
        Serializer.Serialize(Payload, s);
    }

    public Packet<T> Deserialize(Stream s)
    {
        Payload = Serializer.Deserialize<T>(s);
        
        return this;
    }

    public virtual void ProcessClient<T>(NetworkClient<T> client) where T : TransmitterBase<T> {}
    
    public virtual void ProcessServer<T>(NetworkServer<T> server, ClientRef<T> source) where T : TransmitterBase<T> {}
}

/// <summary>
/// An empty payload, use this when you want to send a packet which doesn't have any data (Alternatively, you can use an empty object with [SerializeClass] attribute).
/// </summary>
[SerializeClass]
public class EmptyPayload
{
    
}