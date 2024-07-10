using PacketLib.Base;
using PacketLib.Packet;
using SerializeLib;
using SerializeLib.Attributes;

namespace PacketLib.SharedObject;

public static class SharedRegistry
{
    /// <summary>
    /// Dictionary which allows lookups for linked objects per client/server.
    /// </summary>
    private static Dictionary<object, Dictionary<Guid, SharedObject>> sharedObjects = new ();

    private static Dictionary<object, Dictionary<Type, ObjectCreationListener>> onCreate = new();
    
    public static Dictionary<Guid, SharedObject> GetSharedObjectsFor(object key)
    {
        if (!sharedObjects.TryGetValue(key, out var sharedObject))
        {
            sharedObject = new Dictionary<Guid, SharedObject>();
            sharedObjects.Add(key, sharedObject);
        }
        return sharedObject;
    }

    public static ObjectCreationListener GetOnCreateFor(object key, Type type)
    {
        if (!onCreate.TryGetValue(key, out var eventHandlerDict))
        {
            eventHandlerDict = new Dictionary<Type, ObjectCreationListener>();
            onCreate.Add(key, eventHandlerDict);
        }

        if (!eventHandlerDict.TryGetValue(type, out var eventHandler))
        {
            eventHandler = new ObjectCreationListener();
            eventHandlerDict.Add(type, eventHandler);
        }
        return eventHandler;
    }
}

public static class SharedObjectExtensions
{
    public static void RegisterSharedObjectPackets(this PacketRegistry reg)
    {
        reg.RegisterPacket(typeof(SharedObjectPacket));
        reg.RegisterPacket(typeof(SharedObjectDeltaPacket));
    }

    public static ObjectCreationListener GetOnCreateFor<T>(this NetworkServer<T> key, Type type) where T : TransmitterBase<T>
    {
        return SharedRegistry.GetOnCreateFor(key, type);
    }
    
    public static ObjectCreationListener GetOnCreateFor<T>(this NetworkClient<T> key, Type type) where T : TransmitterBase<T>
    {
        return SharedRegistry.GetOnCreateFor(key, type);
    }

    public static Dictionary<Guid, SharedObject> GetSharedObjects<T>(this NetworkServer<T> key) where T : TransmitterBase<T>
    {
        return SharedRegistry.GetSharedObjectsFor(key);
    }
    
    public static Dictionary<Guid, SharedObject> GetSharedObjects<T>(this NetworkClient<T> key) where T : TransmitterBase<T>
    {
        return SharedRegistry.GetSharedObjectsFor(key);
    }
}

[Flags]
public enum DirectionAllowed
{
    /// <summary>
    /// An object being sent from client to server.
    /// </summary>
    ClientToServer = 1,
    
    /// <summary>
    /// An object being sent from server to client.
    /// </summary>
    ServerToClient = 2,
    
    /// <summary>
    /// An object being forwarded from client to all other clients by the server.
    /// </summary>
    ClientToClient = 4,
    
    /// <summary>
    /// An object being sent back to the sender by the server. Only has an effect if ClientToClient is also used.
    /// </summary>
    IncludeSelf = 8,
}

public abstract class SharedObject
{
    [SerializeField(-1)] // Negative number is used to prevent conflicts
    public Guid Guid = Guid.NewGuid();
    
    public Dictionary<Guid, Action<object?>> RpcCallbacks = new(); // Only used if rpc is used
    
    public SharedObject() {}

    public SharedObject(object registry)
    {
        RegisterLocal(registry);
    }

    public static T CreateRegistered<T>(object registry, params object[] args) where T : SharedObject
    {
        var obj = Activator.CreateInstance(typeof(T), args) as T;
        obj.RegisterLocal(registry);
        return obj;
    }

    public void RegisterLocal(object registry)
    {
        SharedRegistry.GetSharedObjectsFor(registry).Add(Guid, this);
    }
    
    public virtual DirectionAllowed Direction => DirectionAllowed.ClientToServer | DirectionAllowed.ServerToClient | DirectionAllowed.ClientToClient | DirectionAllowed.IncludeSelf;
    
    public virtual void OnCreateClient<T>(NetworkClient<T> client) where T : TransmitterBase<T> {}
    public virtual void OnCreateServer<T>(NetworkServer<T> server) where T : TransmitterBase<T> {}

    public void Send<T>(NetworkServer<T> server) where T : TransmitterBase<T>
    {
        server.SendToAll(SharedObjectPacket.Create(this, GetType()));
    }

    public void Send<T>(NetworkServer<T> server, Guid clientId) where T : TransmitterBase<T>
    {
        server.SendToClient(SharedObjectPacket.Create(this, GetType()), clientId);
    }

    public void Send<T>(ClientRef<T> clientRef) where T : TransmitterBase<T>
    {
        clientRef.Send(SharedObjectPacket.Create(this, GetType()));
    }
    
    public void Send<T>(NetworkClient<T> client) where T : TransmitterBase<T>
    {
        client.Send(SharedObjectPacket.Create(this, GetType()));
    }

    /// <summary>
    /// Mark an update, searches by SerializeField attribute order. Only supports fields, not properties.
    /// </summary>
    /// <param name="order">The order of the SerializeField attribute associated with the set property.</param>
    /// <param name="property">The property to set.</param>
    /// <param name="value">The new value to be set.</param>
    public void MarkUpdateAndSetValue<T>(int order, ref T property, T value)
    {
        property = value;
        _segmentQueue.Add(new SharedObjectDeltaSegment
        {
            FieldSerializeOrder = order,
            FieldValue = Serializer.Serialize(value)
        });
    }
    
    private List<SharedObjectDeltaSegment> _segmentQueue = new();

    public void SendUpdates<T>(NetworkServer<T> server) where T : TransmitterBase<T>
    {
        server.SendToAll(SharedObjectDeltaPacket.FromSegments(Guid, _segmentQueue.ToArray()));
        _segmentQueue.Clear();
    }

    public void SendUpdates<T>(NetworkServer<T> server, Guid clientId) where T : TransmitterBase<T>
    {
        server.SendToClient(SharedObjectDeltaPacket.FromSegments(Guid, _segmentQueue.ToArray()), clientId);
        _segmentQueue.Clear();
    }

    public void SendUpdates<T>(ClientRef<T> clientRef) where T : TransmitterBase<T>
    {
        clientRef.Send(SharedObjectDeltaPacket.FromSegments(Guid, _segmentQueue.ToArray()));
        _segmentQueue.Clear();
    }

    public void SendUpdates<T>(NetworkClient<T> client) where T : TransmitterBase<T>
    {
        client.Send(SharedObjectDeltaPacket.FromSegments(Guid, _segmentQueue.ToArray()));
        _segmentQueue.Clear();
    }
}