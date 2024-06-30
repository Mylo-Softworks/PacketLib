using PacketLib.Base;
using SerializeLib.Attributes;
using SerializeLib.Interfaces;

namespace PacketLib.Packet;

/// <summary>
/// A payload containing a Guid, manually serialized. Can also be used inside another payload.
/// </summary>
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
/// A payload containing a timestamp.
/// </summary>
[SerializeClass]
public class TimePayload
{
    /// <summary>
    /// The timestamp.
    /// </summary>
    [SerializeField] public long Time;
    
    public TimePayload() {}

    public TimePayload(long time)
    {
        Time = time;
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
        client.OnConnect();
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

/// <summary>
/// A ping packet which indicates the ping between client and server.
///
/// On client: Compare the current time with the time in the packet to calculate the ping.
/// On server: Compare the current time with the time in the packet to calculate the ping, then respond to the client with the current time.
/// </summary>
public class Ping : Packet<TimePayload>
{
    /// <summary>
    /// Helper function to get the current timestamp for calculating ping.
    /// </summary>
    /// <returns>The current time in MS.</returns>
    public static long GetCurrentTimeStamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// Helper function to create a Ping packet with the current time.
    /// </summary>
    /// <returns>The created Ping packet.</returns>
    public static Ping CreateWithCurrent() =>
        new Ping
        {
            Payload = new TimePayload(GetCurrentTimeStamp())
        };

    /// <summary>
    /// Calculate the ping on this packet.
    /// </summary>
    /// <returns>The calculated ping value.</returns>
    public long Compare()
    {
        return GetCurrentTimeStamp() - Payload.Time;
    }

    public override void ProcessClient<T>(NetworkClient<T> client)
    {
        var ping = Compare();
        client.Transmitter.Ping = (int) ping;
        client.Transmitter.LastPingTime = DateTime.UtcNow;
    }

    public override void ProcessServer<T>(NetworkServer<T> server, ClientRef<T> source)
    {
        var ping = Compare();
        source.Transmitter.Ping = (int) ping;
        source.Transmitter.LastPingTime = DateTime.UtcNow;
        source.Send(CreateWithCurrent()); // Reply
    }
}