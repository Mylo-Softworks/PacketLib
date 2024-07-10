using System.Net.Sockets;
using System.Reflection;
using PacketLib.Base;
using PacketLib.Packet;
using PacketLib.RPC;
using PacketLib.RPC.Attributes;
using PacketLib.SharedObject;
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

[SerializeClass]
public class TestObject : SharedObject
{
    [SerializeField(0)] public string Content;
    
    public TestObject() {}
    public TestObject(object registry) : base(registry) {}

    public string SharedContent
    {
        get => Content;
        set => MarkUpdateAndSetValue(0, ref Content, value);
    }

    public override DirectionAllowed Direction => DirectionAllowed.ClientToServer | DirectionAllowed.ServerToClient;

    [RPC(DirectionAllowed.ServerToClient)]
    public void BasicRpcFun()
    {
        Console.WriteLine("Triggered BasicRpcFun");
    }

    [RPC(DirectionAllowed.ServerToClient)]
    public void ClientPrintRpc(string message)
    {
        Console.WriteLine(message);
    }

    [RPC(DirectionAllowed.ClientToServer)]
    public string GetMessageFromServer()
    {
        return "Message from server!";
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

    public static void Rpc()
    {
        var reg = new PacketRegistry();
        
        reg.RegisterSharedObjectAndRpcPackets();

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

        server.GetOnCreateFor(typeof(TestObject)).onCreate += (sender, obj) =>
        {
            var testObj = (obj as TestObject)!;
            Console.WriteLine("Created on server: " + testObj.Content);

            testObj.SharedContent = "This is an edited message!";
            testObj.SendUpdates(server);

            // testObj.CallRpc(nameof(testObj.BasicRpcFun), server);
            
            testObj.CallRpc(nameof(testObj.ClientPrintRpc), server, "This is a message!");
        };
            
        
        server.Start(1337, false);
        
        Thread.Sleep(100);
        
        client.Connect("127.0.0.1", 1337);
        
        Thread.Sleep(100);
        
        client.Poll();
        
        Thread.Sleep(100);
        
        server.Poll();
        
        // var clientSideTestObject = new TestObject(client) { Content = "This is a test!" };
        var clientSideTestObject = SharedObject.CreateRegistered<TestObject>(client);
        clientSideTestObject.Content = "This is a test!";
        clientSideTestObject.Send(client); // Send to server
        
        clientSideTestObject.CallRpc(nameof(clientSideTestObject.GetMessageFromServer), o => Console.WriteLine(o), client);
        
        client.Poll();
        
        Thread.Sleep(100);
        
        server.Poll();
        
        Thread.Sleep(100);
        
        client.Poll();
        
        Console.WriteLine("Updated on client: " + clientSideTestObject.Content);

        // Console.WriteLine((sharedObject as TestObject).Content);
    }
}