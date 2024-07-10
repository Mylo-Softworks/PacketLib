using System.Reflection;
using PacketLib.Base;
using PacketLib.Packet;
using PacketLib.RPC.Attributes;
using PacketLib.SharedObject;

namespace PacketLib.RPC;

public static class SharedObjectExtensions
{
    public static void RegisterRpcPackets(this PacketRegistry reg)
    {
        reg.RegisterPacket(typeof(RpcCallPacket));
        reg.RegisterPacket(typeof(RpcResponsePacket));
    }

    public static void RegisterSharedObjectAndRpcPackets(this PacketRegistry reg)
    {
        reg.RegisterSharedObjectPackets();
        reg.RegisterRpcPackets();
    }

    public static (MethodInfo?, DirectionAllowed) GetRpcMethod(this SharedObject.SharedObject sharedObject, string methodName, Type[] types)
    {
        var type = sharedObject.GetType();

        var method = type.GetMethod(methodName, types);

        var attrib = method?.GetCustomAttribute<RPCAttribute>();

        if (attrib == null) return (null, 0);
        
        return (method, attrib.DirectionAllowed);
    }

    public static void CallRpc<T>(this SharedObject.SharedObject sharedObject, string methodName, NetworkServer<T> server, params object[] args) where T : TransmitterBase<T>
    {
        server.SendToAll(RpcCallPacket.Create(sharedObject, methodName, args));
    }
    
    public static void CallRpc<T>(this SharedObject.SharedObject sharedObject, string methodName, NetworkServer<T> server, Guid clientId, params object[] args) where T : TransmitterBase<T>
    {
        server.SendToClient(RpcCallPacket.Create(sharedObject, methodName, args), clientId);
    }
    
    public static void CallRpc<T>(this SharedObject.SharedObject sharedObject, string methodName, ClientRef<T> clientRef, params object[] args) where T : TransmitterBase<T>
    {
        clientRef.Send(RpcCallPacket.Create(sharedObject, methodName, args));
    }
    
    public static void CallRpc<T>(this SharedObject.SharedObject sharedObject, string methodName, NetworkClient<T> client, params object[] args) where T : TransmitterBase<T>
    {
        client.Send(RpcCallPacket.Create(sharedObject, methodName, args));
    }
    
    
    public static void CallRpc<T>(this SharedObject.SharedObject sharedObject, string methodName, Action<object> callback, NetworkServer<T> server, params object[] args) where T : TransmitterBase<T>
    {
        server.SendToAll(RpcCallPacket.Create(sharedObject, methodName, args).RegisterCallback(callback));
    }
    
    public static void CallRpc<T>(this SharedObject.SharedObject sharedObject, string methodName, Action<object> callback, NetworkServer<T> server, Guid clientId, params object[] args) where T : TransmitterBase<T>
    {
        server.SendToClient(RpcCallPacket.Create(sharedObject, methodName, args).RegisterCallback(callback), clientId);
    }
    
    public static void CallRpc<T>(this SharedObject.SharedObject sharedObject, string methodName, Action<object> callback, ClientRef<T> clientRef, params object[] args) where T : TransmitterBase<T>
    {
        clientRef.Send(RpcCallPacket.Create(sharedObject, methodName, args).RegisterCallback(callback));
    }
    
    public static void CallRpc<T>(this SharedObject.SharedObject sharedObject, string methodName, Action<object> callback, NetworkClient<T> client, params object[] args) where T : TransmitterBase<T>
    {
        client.Send(RpcCallPacket.Create(sharedObject, methodName, args).RegisterCallback(callback));
    }
}