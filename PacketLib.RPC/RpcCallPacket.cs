using PacketLib.Base;
using PacketLib.Packet;
using PacketLib.SharedObject;
using SerializeLib.Attributes;

namespace PacketLib.RPC;

[SerializeClass]
public class RpcCallPayload
{
    [SerializeField(0)] public bool Forwarded;
    [SerializeField(1)] public Guid SharedObjectRef;
    public SharedObject.SharedObject SharedObject;
    [SerializeField(2)] public Guid RpcRequestId;
    [SerializeField(3)] public string MethodName;
    [SerializeField(4)] public UnknownTypePayload[] Args;
}

public class RpcCallPacket : Packet<RpcCallPayload>
{
    public static RpcCallPacket Create(SharedObject.SharedObject sharedObject, string methodName, object[] args)
    {
        var guid = Guid.NewGuid();
        return new RpcCallPacket
        {
            Payload = new RpcCallPayload
            {
                Forwarded = false,
                SharedObjectRef = sharedObject.Guid,
                SharedObject = sharedObject, // Used only locally, so not serialized
                RpcRequestId = guid,
                MethodName = methodName,
                Args = args.Select(o => UnknownTypePayload.Create(o.GetType(), o)).ToArray()
            }
        };
    }

    public RpcCallPacket RegisterCallback(Action<object> callback)
    {
        Payload.SharedObject.RpcCallbacks[Payload.RpcRequestId] = callback;
        return this;
    }
    
    public override void ProcessClient<T>(NetworkClient<T> client)
    {
        var sharedObjects = client.GetSharedObjects();
        if (!sharedObjects.TryGetValue(Payload.SharedObjectRef, out var value)) return;
        
        var (methodInfo, dir) = value.GetRpcMethod(Payload.MethodName, Payload.Args.Select(payload => payload.Type!).ToArray());
        if (methodInfo == null) return;
        
        var forwarded = Payload.Forwarded;

        if ((!forwarded && (dir & DirectionAllowed.ServerToClient) != 0) ||
            (forwarded && (dir & DirectionAllowed.ClientToClient) != 0))
        {
            var result = methodInfo?.Invoke(value, Payload.Args.Select(payload => payload.Payload).ToArray());
            
            var returnType = methodInfo!.ReturnType;
            if (returnType != typeof(void))
            {
                client.Send(RpcResponsePacket.Create(Payload.RpcRequestId, Payload.SharedObjectRef, result, methodInfo.ReturnType));
            }
        }
    }

    public override void ProcessServer<T>(NetworkServer<T> server, ClientRef<T> source)
    {
        var sharedObjects = server.GetSharedObjects();
        if (!sharedObjects.TryGetValue(Payload.SharedObjectRef, out var value)) return;
        
        var (methodInfo, dir) = value.GetRpcMethod(Payload.MethodName, Payload.Args.Select(payload => payload.Type!).ToArray());
        if (methodInfo == null) return;
        var forwarded = Payload.Forwarded;
        
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
            var result = methodInfo?.Invoke(value, Payload.Args.Select(payload => payload.Payload).ToArray());
            
            var returnType = methodInfo!.ReturnType;
            if (returnType != typeof(void))
            {
                source.Send(RpcResponsePacket.Create(Payload.RpcRequestId, Payload.SharedObjectRef, result, methodInfo.ReturnType));
            }
        }
    }
}