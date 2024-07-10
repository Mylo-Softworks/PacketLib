using SerializeLib.Interfaces;

namespace PacketLib.Util;

public class GuidSerializeOverride : ISerializableOverride<Guid>
{
    private int size = 16;

    public void Serialize(Guid target, Stream s)
    {
        s.Write(target.ToByteArray());
    }

    public Guid Deserialize(Stream s)
    {
        var buffer = new byte[size];
        s.Read(buffer, 0, buffer.Length);
        return new Guid(buffer);
    }
}