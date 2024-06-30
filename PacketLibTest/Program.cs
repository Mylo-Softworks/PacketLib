using System.Net.Sockets;
using System.Reflection;
using PacketLib.Base;
using PacketLib.Packet;
using PacketLib.Transmitters;
using SerializeLib.Attributes;

namespace PacketLibTest;

public class TestPacket : Packet<string>
{
    public override void ProcessServer<T>(NetworkServer<T> server, ClientRef<T> source)
    {
        Console.WriteLine($"Message received! {Payload}");
    }
}

public static class Tests
{
    static void Main()
    {
        var reg = new PacketRegistry();
        
        reg.RegisterAssembly(Assembly.GetExecutingAssembly());

        var server = new NetworkServer<UdpTransmitter>(reg);
        var client = new NetworkClient<UdpTransmitter>(reg);

        server.ClientConnected += (sender, @ref) =>
        {
            Console.WriteLine($"[Server] Client connected: {@ref.Guid}!");
        };

        client.ClientConnected += (sender, guid) =>
        {
            Console.WriteLine($"[Client] Client connected! {guid}");
        };
        
        server.Start(1337, false);
        
        Thread.Sleep(100);
        
        client.Connect("127.0.0.1", 1337);
        
        Thread.Sleep(100);
        
        client.Poll();
        
        Thread.Sleep(100);
        
        server.Poll();
        
        client.Send(new TestPacket() {Payload = "This is a test!"});
        
        Thread.Sleep(1000);
        
        client.Poll(); // Will send ping
        
        Thread.Sleep(50);
        server.Poll(); // Will receive client ping
        
        Thread.Sleep(50);
        client.Poll(); // Will receive server ping
    }
}