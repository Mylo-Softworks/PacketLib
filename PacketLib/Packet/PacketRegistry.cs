using System.Net.Sockets;
using System.Reflection;
using SerializeLib;

namespace PacketLib.Packet;

/// <summary>
/// A registry containing registered packet ids and their corresponding types.
/// </summary>
public class PacketRegistry
{
    private readonly Dictionary<ushort, Type> _packets = new()
    {
        { 0, typeof(Connect) },
        { 1, typeof(Disconnect) },
        { 2, typeof(Ping) }
    };

    int _packetIdSize = sizeof(ushort);
    int _sizeHeaderSize = sizeof(int);

    private ushort _lastIndex = 0;
    
    /// <summary>
    /// Register a packet type.
    /// </summary>
    /// <param name="packet">The packet type to register.</param>
    public void RegisterPacket(Type packet)
    {
        _packets.Add(_lastIndex++, packet);
    }

    /// <summary>
    /// Register a packet type at a specified index.
    /// Counting will continue from this index.
    /// </summary>
    /// <param name="packet">The packet type to register.</param>
    /// <param name="index">The index to register at.</param>
    public void ForceRegisterPacket(Type packet, ushort index)
    {
        _lastIndex = index;
        RegisterPacket(packet);
    }

    /// <summary>
    /// Register every Packet class found in the assembly.
    /// </summary>
    /// <param name="assembly">The assembly to register the Packet classes from.</param>
    public void RegisterAssembly(Assembly assembly)
    {
        assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Packet<>))).ToList().ForEach(RegisterPacket);
    }
    
    public Type this[ushort index] => _packets[index];
    
    // Serializing and deserializing
    
    // Format:
    // [size|packetID|data]
    // size = _packetIdSize + data.size
    
    /// <summary>
    /// Serialize a packet to the stream in the format [size|packetID|data].
    /// </summary>
    /// <param name="packet">The packet object to serialize.</param>
    /// <param name="s">The stream to write to.</param>
    /// <typeparam name="T">The packet payload, this doesn't have to be explicitly defined.</typeparam>
    public void SerializePacket<T>(Packet<T> packet, Stream s)
    {
        var id = _packets.FirstOrDefault(x => x.Value == packet.GetType()).Key;
        using var tempStream = new MemoryStream();
        
        tempStream.Write(BitConverter.GetBytes(id)); // Write the packet id to temp stream
        Serializer.Serialize(packet, tempStream); // Write packet data to temp stream

        int size = (int) tempStream.Length; // Get temp stream size
        tempStream.Seek(0, SeekOrigin.Begin);
        
        s.Write(BitConverter.GetBytes(size)); // Write temp stream size to serialize stream
        tempStream.CopyTo(s); // Write temp stream content to serialize stream
    }

    // Buffer which holds stream until this point. The start of this buffer should always
    private MemoryStream _buffer = new MemoryStream();
    private int _currentSize = -1; // -1 if no size read yet

    /// <summary>
    /// Read all packet data and deserialize the packets found from the stream (NetworkStream supported).
    /// </summary>
    /// <param name="s">The stream to read from.</param>
    /// <returns>Null if no packets could be deserialized, otherwise a list of all packets which have been deserialized.</returns>
    public List<dynamic>? ReadPacketDataUntilThisPoint(Stream s)
    {
        if (s is NetworkStream ns)
        {
            while (ns.DataAvailable)
            {
                _buffer.WriteByte((byte)ns.ReadByte());
            }
        }
        else
        {
            s.CopyTo(_buffer); // This should be the only reference to s.
        }

        if (_currentSize == -1)
        {
            // Check if size can be read
            _buffer.Seek(0, SeekOrigin.Begin);
            
            if (_buffer.Length < _sizeHeaderSize) // Header not readable yet.
                return null;
            
            var buffer = new byte[_sizeHeaderSize];
            _buffer.Read(buffer, 0, _sizeHeaderSize);
            _currentSize = BitConverter.ToInt32(buffer, 0);
        }

        // _currentSize won't be -1 now
        var neededSize = _currentSize + _sizeHeaderSize; // since packet will be [size|rest]

        if (_buffer.Length < neededSize) return null; // Full packet not readable yet.

        _currentSize = -1; // Don't forget to reset!
        
        // Full packet will be available now.
        _buffer.Seek(_sizeHeaderSize, SeekOrigin.Begin); // Start after the header
        
        // Store the first neededSize packets in thisPacketStream, and remove them from the start of _buffer
        var writeBuffer = new byte[neededSize];
        
        _buffer.Read(writeBuffer, 0, neededSize);

        var newBuffer = new MemoryStream(); // New buffer which gets the data after this packet.
        _buffer.Seek(neededSize, SeekOrigin.Begin);
        _buffer.CopyTo(newBuffer);
        _buffer = newBuffer;
        
        var thisPacketStream = new MemoryStream(writeBuffer);
        thisPacketStream.Seek(0, SeekOrigin.Begin);
        // Read packet type
        var typeBuffer = new byte[_packetIdSize];
        thisPacketStream.Read(typeBuffer, 0, _packetIdSize); // Offset
        var type = BitConverter.ToUInt16(typeBuffer, 0);

        var packet = DeserializePacket(thisPacketStream, type);
        
        var outList = new List<dynamic>();
        outList.Add(packet);

        var recursiveResult = ReadPacketDataUntilThisPoint(s);
        if (recursiveResult != null)
        {
            outList.AddRange(recursiveResult);
        }
        
        return outList;
    }

    /// <summary>
    /// Deserialize a single packet based on it's id from a stream.
    /// </summary>
    /// <param name="s">The stream to read.</param>
    /// <param name="type">The packet id registered in this PacketRegistry.</param>
    /// <returns>A dynamic containing the deserialized packet.</returns>
    public dynamic DeserializePacket(Stream s, ushort type) =>
        Serializer.Deserialize(s, _packets[type])!;
    
}