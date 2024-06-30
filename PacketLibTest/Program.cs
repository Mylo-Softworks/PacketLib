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

        server.ClientConnected += (sender, @ref) =>
        {
            Console.WriteLine($"[Server] Client connected: {@ref.Guid}!");
        };

        client.ClientConnected += (sender, guid) =>
        {
            Console.WriteLine($"[Client] Client connected! {guid}");
        };
        
        server.Start(1337);
        
        Thread.Sleep(100);
        
        client.Connect("127.0.0.1", 1337);
        
        Thread.Sleep(100);
        
        client.Poll();
        
        Thread.Sleep(100);
        
        server.Poll();
        
        Thread.Sleep(1000);
        
        client.Poll(); // Will send ping
        server.Poll(); // Will receive client ping
        client.Poll(); // Will receive server ping
    }
}