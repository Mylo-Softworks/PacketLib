using PacketLib.Base;
using PacketLib.Packet;
using PacketLib.SharedObject;
using SerializeLib.Attributes;

namespace PacketLib.RPC;

[SerializeClass]
public class RpcResponsePayload
{
    [SerializeField(0)] public Guid RpcRequestId;
    [SerializeField(1)] public Guid SharedObjectId;
    [SerializeField(2)] public UnknownTypePayload Result;
}

public class RpcResponsePacket : Packet<RpcResponsePayload>
{
    public static RpcResponsePacket Create(Guid rpcRequestId, Guid sharedObjectId, object? result, Type resultType)
    {
        return new RpcResponsePacket
        {
            Payload = new RpcResponsePayload
            {
                RpcRequestId = rpcRequestId,
                SharedObjectId = sharedObjectId,
                Result = UnknownTypePayload.Create(resultType, result),
            }
        };
    }

    public override void ProcessClient<T>(NetworkClient<T> client)
    {
        var sharedObjects = client.GetSharedObjects();

        if (!sharedObjects.TryGetValue(Payload.SharedObjectId, out var sharedObject)) return;

        if (!sharedObject.RpcCallbacks.TryGetValue(Payload.RpcRequestId, out var rpcCallback)) return;

        rpcCallback(Payload.Result.Payload);
    }

    public override void ProcessServer<T>(NetworkServer<T> server, ClientRef<T> source)
    {
        var sharedObjects = server.GetSharedObjects();

        if (!sharedObjects.TryGetValue(Payload.SharedObjectId, out var sharedObject)) return;
        
        if (!sharedObject.RpcCallbacks.TryGetValue(Payload.RpcRequestId, out var rpcCallback)) return;

        rpcCallback(Payload.Result.Payload);
    }
}