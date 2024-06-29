using System.Runtime.InteropServices;
using PacketLib.Base;
using SerializeLib.Attributes;
using SerializeLib.Interfaces;

namespace PacketLib.Packet;

public class GuidPayload : ISerializableClass<GuidPayload>
{
    public Guid Guid;

    private int size = 16; // https://learn.microsoft.com/en-us/dotnet/api/system.guid.tobytearray
    
    public void Serialize(Stream s)
    {
        s.Write(Guid.ToByteArray());
    }

    public GuidPayload Deserialize(Stream s)
    {
        var buffer = new byte[size];
        s.Read(buffer, 0, buffer.Length);
        Guid = new Guid(buffer);

        return this;
    }
}

/// <summary>
/// Connect is a packet available both on client and server.
/// 
/// On client: Tells the client their Guid, and confirms a successful connection.
/// </summary>
public class Connect : Packet<GuidPayload>
{
    public override void ProcessClient<T>(NetworkClient<T> client)
    {
        client.Guid = Payload.Guid; // Guid is now known
    }
}

/// <summary>
/// Disconnect is a packet available both on client and server.
///
/// On client: Tells the client that the server disconnected the client, makes it clear that the connection has been closed.
/// On server: Tells the server that the client disconnected from the server, makes it clear that the connection has been closed.
/// </summary>
public class Disconnect : Packet<EmptyPayload>
{
    public override void ProcessClient<T>(NetworkClient<T> client)
    {
        client.OnDisconnect();
    }

    public override void ProcessServer<T>(NetworkServer<T> server, ClientRef<T> source)
    {
        server.OnDisconnect(source);
    }
}