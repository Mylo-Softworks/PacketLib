using System.Net.Sockets;
using System.Reflection;
using PacketLib.Base;
using PacketLib.Packet;
using PacketLib.Transmitters;

namespace PacketLibTest;

public static class Tests
{
    static void Main()
    {
        var reg = new PacketRegistry();
        
        reg.RegisterAssembly(Assembly.GetExecutingAssembly());

        var server = new NetworkServer<TcpTransmitter>(reg);
        var client = new NetworkClient<TcpTransmitter>(reg);
    }
}