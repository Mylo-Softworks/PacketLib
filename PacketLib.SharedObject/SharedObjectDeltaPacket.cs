using System.Reflection;
using PacketLib.Base;
using PacketLib.Packet;
using SerializeLib;
using SerializeLib.Attributes;

namespace PacketLib.SharedObject;

[SerializeClass]
public class SharedObjectDeltaSegment
{
    [SerializeField(0)] public int FieldSerializeOrder;
    [SerializeField(1)] public byte[] FieldValue;
}

[SerializeClass]
public class SharedObjectDeltaPayload
{
    [SerializeField(0)] public bool Forwarded;
    
    /// <summary>
    /// The object this is a delta of.
    /// </summary>
    [SerializeField(1)] public Guid TargetObject;
    
    [SerializeField(2)] public SharedObjectDeltaSegment[] Segments = new SharedObjectDeltaSegment[] {}; // Delta packet supports multiple changes if needed

    public static SharedObjectDeltaSegment[] MergeDuplicates(SharedObjectDeltaSegment[] segments)
    {
        var outDict = new Dictionary<int, SharedObjectDeltaSegment>();
        foreach (var segment in segments)
        {
            outDict[segment.FieldSerializeOrder] = segment;
        }

        return outDict.Values.ToArray();
    }
}

public class SharedObjectDeltaPacket : Packet<SharedObjectDeltaPayload>
{
    public static SharedObjectDeltaPacket FromSegments(Guid guid, SharedObjectDeltaSegment[] segments)
    {
        return new SharedObjectDeltaPacket
        {
            Payload = new SharedObjectDeltaPayload
            {
                Forwarded = false,
                TargetObject = guid,
                Segments = SharedObjectDeltaPayload.MergeDuplicates(segments)
            }
        };
    }

    private void ApplyChanges(Type type, SharedObject sharedObject, SharedObjectDeltaSegment[] segments)
    {
        foreach (var segment in segments)
        {
            var targettedId = segment.FieldSerializeOrder;
            var newValue = segment.FieldValue;
            
            foreach (var fieldInfo in type.GetFields())
            {
                var attribute = fieldInfo.GetCustomAttribute(typeof(SerializeFieldAttribute));
                if (attribute == null) continue;
                
                var serializeFieldAttribute = (attribute as SerializeFieldAttribute)!;
                var order = serializeFieldAttribute.Order;
                if (targettedId == order) // Target found
                {
                    var newValueDeserialized = Serializer.Deserialize(newValue, fieldInfo.FieldType);
                    fieldInfo.SetValue(sharedObject, newValueDeserialized); // Apply delta
                }
            }
        }
    }
    
    public override void ProcessClient<T>(NetworkClient<T> client)
    {
        var sharedObjects = client.GetSharedObjects();

        if (!sharedObjects.ContainsKey(Payload.TargetObject)) return;

        var obj = sharedObjects[Payload.TargetObject];

        var dir = obj.Direction;
        var forwarded = Payload.Forwarded;

        if ((!forwarded && (dir & DirectionAllowed.ServerToClient) != 0) ||
            (forwarded && (dir & DirectionAllowed.ClientToClient) != 0))
        {
            ApplyChanges(obj.GetType(), obj, Payload.Segments);
        }
    }

    public override void ProcessServer<T>(NetworkServer<T> server, ClientRef<T> source)
    {
        var sharedObjects = server.GetSharedObjects();

        if (!sharedObjects.ContainsKey(Payload.TargetObject)) return;
        
        var obj = sharedObjects[Payload.TargetObject];
        
        var dir = obj.Direction;
        
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
            ApplyChanges(obj.GetType(), obj, Payload.Segments);
        }
    }
}