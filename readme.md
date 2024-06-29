﻿# PacketLib
A library for high level packet-based TCP/UDP networking in .net.

## High level?
PacketLib is high level as it allows developers to easily write and register new packets and start a server.

## Packet based?
PacketLib uses classes named "Packets" which contain data to transfer over the selected communication protocol.  
PacketLib takes an object-oriented approach, using [SerializeLib] to serialize and deserialize the data.

# Technical

## Packet format
Packets are structured simply \[size|identifier|content]
* Size is a 32-bit integer containing the size of the full packet's identifier + content. This is used for reading the stream properly.
* Identifier contains a 16-bit unsigned integer (ushort) which contains the packet id for the lookup table (In PacketRegistry).
* Content is the [SerializeLib] serialized representation of the packet object.

[SerializeLib]: https://github.com/Mylo-Softworks/SerializeLib/