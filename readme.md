# PacketLib
A protocol-agnostic library for high level packet-based networking in .net.

## Protocol agnostic?
Protocols are defined as a class which implements `TransmitterBase`. The `TransmitterBase` class has functions for reading and writing the protocol. Including connecting and hosting.

## High level?
PacketLib is high level as it allows developers to easily write and register new packets and start a server.

## Packet based?
PacketLib uses classes named "Packets" which contain data to transfer over the selected communication protocol.  
PacketLib takes an object-oriented approach, using [SerializeLib] to serialize and deserialize the data.

# Usage

## Creating packets
Packets are simply classes which inherit `Packet`. They contain a payload of which the type can be specified as the generic argument.
Packets and payloads can have custom constructors, but there should always be one parameterless constructor. This is used for deserializing.
```csharp
using PacketLib.Packet;
using SerializeLib.Interfaces; // For payloads

// To create a packet without a payload, set the generic type parameter to EmptyPayload
class ExamplePacketWithoutPayload : Packet<EmptyPayload>
{
    // Optional, will be empty if not overriden
    public override void ProcessClient<T>(NetworkClient<T> client)
    {
        // Client logic goes here
    }

    // Optional, will be empty if not overriden
    public override void ProcessServer<T>(NetworkServer<T> server, ClientRef<T> source)
    {
        // Server logic goes here
    }
}

// To create a payload, use serializelib to make the packet serializable.
[SerializeClass]
class ExamplePayload {
    [SerializeField]
    public int Number;
}

class ExamplePacketWithPayload : Packet<ExamplePayload> {
    // Optional, will be empty if not overriden
    public override void ProcessClient<T>(NetworkClient<T> client)
    {
        var number = Payload.Number; // Access the payload
    }
}
```

## Registering packets
```csharp
using PacketLib.Packet;

// Creating a PacketRegistry
var reg = new PacketRegistry();

// Registering a packet by type
reg.RegisterPacket(typeof(ExamplePacketWithoutPayload));

// Auto register all packets in this assembly
reg.RegisterAssembly(Assembly.GetExecutingAssembly());
```

## Creating and using a server
```csharp
using PacketLib.Base; // Client and server classes
using PacketLib.Transmitters; // Default transmitters

var server = new NetworkServer<TcpTransmitter>(reg); // Creates a tcp server

server.ClientConnected += (sender, @ref) =>
{
    Console.WriteLine($"[Server] Client connected: {@ref.Guid}!");
};

// To process the current queue of packets
server.Poll();

// To send a packet to all clients
server.SendToAll(new ExamplePacketWithoutPayload());

// Sending by ClientRef (called "client" here)
client.Send(new ExamplePacketWithoutPayload());

// Sending by Guid (called "Guid" here)
// The result of this call is a boolean which indicates if the packet could actually be sent. (It won't send to a client which is not registered to this server.)
var sent = server.SendToClient(new ExamplePacketWithoutPayload(), Guid);
```

## Creating and using a client
```csharp
using PacketLib.Base; // Client and server classes
using PacketLib.Transmitters; // Default transmitters

var client = new NetworkClient<TcpTransmitter>(reg); // Creates a tcp client

client.ClientConnected += (sender, guid) =>
{
    Console.WriteLine($"[Client] Client connected! {guid}");
};

// To process the current queue of packets
client.Poll();

// To send a packet to the server
client.Send(new ExamplePacketWithoutPayload());
```

# Technical

## Packet format
Packets are structured simply \[size|identifier|content]
* Size is a 32-bit integer containing the size of the full packet's identifier + content. This is used for reading the stream properly.
* Identifier contains a 16-bit unsigned integer (ushort) which contains the packet id for the lookup table (In PacketRegistry).
* Content is the [SerializeLib] serialized representation of the packet object.

[SerializeLib]: https://github.com/Mylo-Softworks/SerializeLib/