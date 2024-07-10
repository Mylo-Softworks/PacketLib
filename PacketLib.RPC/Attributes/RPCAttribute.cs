using PacketLib.SharedObject;

namespace PacketLib.RPC.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class RPCAttribute : Attribute
{
    public DirectionAllowed DirectionAllowed;
    public RPCAttribute(DirectionAllowed directionAllowed)
    {
        DirectionAllowed = directionAllowed;
    }
}