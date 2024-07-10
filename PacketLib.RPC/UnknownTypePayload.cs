using SerializeLib;
using SerializeLib.Attributes;
using SerializeLib.Interfaces;

namespace PacketLib.RPC;

public class UnknownTypePayload : ISerializableClass<UnknownTypePayload>
{
    public Type? Type;
    public object? Payload;

    public static UnknownTypePayload Create<T>(T payload)
    {
        return new UnknownTypePayload
        {
            Type = typeof(T),
            Payload = payload
        };
    }

    public static UnknownTypePayload Create(Type t, object? payload)
    {
        return new UnknownTypePayload
        {
            Type = t,
            Payload = payload
        };
    }

    public void Serialize(Stream s)
    {
        Serializer.SerializeValue(Type!.AssemblyQualifiedName!, s);
        Serializer.SerializeValue(Payload, s, Type);
    }

    public UnknownTypePayload Deserialize(Stream s)
    {
        Type = Type.GetType(Serializer.DeserializeValue<string>(s)!)!;
        Payload = Serializer.DeserializeValue(s, Type)!;

        return this;
    }
}