using PacketLib.Base;
using PacketLib.Packet;
using SerializeLib;
using SerializeLib.Interfaces;

namespace PacketLib.SharedObject;

public class SharedObjectPayload : ISerializableClass<SharedObjectPayload>
{
    public bool Forwarded; // Indicates if this packet originates from a client (true) or the server (false)
    public SharedObject? Object;
    public Type? Type;
    
    public void Serialize(Stream s)
    {
        Serializer.SerializeValue(Forwarded, s);
        Serializer.SerializeValue(Type!.AssemblyQualifiedName!, s);
        Serializer.SerializeValue(Object!, s, Type);
    }

    public SharedObjectPayload Deserialize(Stream s)
    {
        Forwarded = Serializer.DeserializeValue<bool>(s);
        Type = Type.GetType(Serializer.DeserializeValue<string>(s)!)!;
        Object = (SharedObject) Serializer.DeserializeValue(s, Type)!;

        return this;
    }
}

/// <summary>
/// A packet which contains a serializable payload.
/// </summary>
public class SharedObjectPacket : Packet<SharedObjectPayload>
{
    public static SharedObjectPacket Create<T>(T obj) where T : SharedObject
    {
        return Create(obj, typeof(T));
    }

    public static SharedObjectPacket Create(SharedObject? obj, Type type)
    {
        return new SharedObjectPacket
        {
            Payload = new SharedObjectPayload
            {
                Forwarded = false, // Default
                Object = obj,
                Type = type,
            }
        };
    }

    public SharedObject Obj => Payload.Object!;

    public override void ProcessClient<T>(NetworkClient<T> client)
    {
        var forwarded = Payload.Forwarded;
        var dir = Obj.Direction;

        if ((!forwarded && (dir & DirectionAllowed.ServerToClient) != 0) ||
            (forwarded && (dir & DirectionAllowed.ClientToClient) != 0))
        {
            var obj = Obj;
            SharedRegistry.GetSharedObjectsFor(client).Add(obj.Guid, obj);
            obj.OnCreateClient(client);
            client.GetOnCreateFor(obj.GetType()).Call(obj);
        }
    }

    public override void ProcessServer<T>(NetworkServer<T> server, ClientRef<T> source)
    {
        var dir = Obj.Direction;

        if ((dir & DirectionAllowed.ClientToClient) != 0)
        {
            var includeSelf = (dir & DirectionAllowed.IncludeSelf) != 0;
            
            Payload.Forwarded = true; // Mark packet as forwarded
            
            // Redirect to other clients
            if (includeSelf)
            {
                server.SendToAll(this);
            }
            else
            {
                foreach (var (guid, client) in server.Clients.Where(pair => pair.Value.Guid != source.Guid))
                {
                    client.Send(this);
                }
            }
        }

        if ((dir & DirectionAllowed.ClientToServer) != 0)
        {
            var obj = Obj;
            SharedRegistry.GetSharedObjectsFor(server).Add(obj.Guid, obj);
            obj.OnCreateServer(server);
            server.GetOnCreateFor(obj.GetType()).Call(obj);
        }
    }
}