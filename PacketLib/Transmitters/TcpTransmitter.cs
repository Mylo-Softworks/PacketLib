﻿using System.Net;
using System.Net.Sockets;
using PacketLib.Base;
using PacketLib.Packet;

namespace PacketLib.Transmitters;

public class TcpTransmitter : TransmitterBase<TcpTransmitter>
{
    private TcpClient? _tcpClient = null;
    private TcpListener? _tcpListener = null;
    
    private IAsyncResult? _connectingAsyncResult = null;

    protected override void InitClientRefImpl(object transfer)
    {
        _tcpClient = (TcpClient)transfer;
    }

    protected override void ConnectImpl(IPEndPoint host)
    {
        _tcpClient = new TcpClient();
        _connectingAsyncResult = _tcpClient.BeginConnect(host.Address, host.Port, ar =>
        {
            if (_connectingAsyncResult == null) return;
            
            _tcpClient.EndConnect(_connectingAsyncResult);
            OnNewClientConnection();
        }, host);
    }

    protected override void HostImpl(IPEndPoint host)
    {
        _tcpListener = new TcpListener(host);
        _tcpListener.Start();

        _tcpListener.BeginAcceptTcpClient(AcceptLoop, null);
    }

    private void AcceptLoop(IAsyncResult result)
    {
        if (_tcpListener == null) return;
        _tcpListener.BeginAcceptTcpClient(AcceptLoop, null);
        var tcpClient = _tcpListener.EndAcceptTcpClient(result);
        
        var tcpClientTransmitter = new TcpTransmitter();
        tcpClientTransmitter.InitClientRef(tcpClient);
        
        OnNewServerConnection((tcpClient.Client.RemoteEndPoint as IPEndPoint)!, tcpClientTransmitter);
    }

    protected override void DisconnectImpl()
    {
        _tcpClient?.Close();
    }

    protected override void SendImpl(Action<Stream> streamWrite)
    {
        if (_tcpClient == null) return;
        
        streamWrite(_tcpClient.GetStream());
    }

    protected override List<Packet<object>>? PollImpl(PacketRegistry registry)
    {
        if (_tcpClient == null) return null;

        return registry.ReadPacketDataUntilThisPoint(_tcpClient.GetStream());
    }

    public override bool IsConnected()
    {
        return _tcpClient?.Connected ?? false;
    }

    public override bool IsConnecting()
    {
        return _connectingAsyncResult != null && !IsConnected();
    }

    public override bool ShouldQueueRemove()
    {
        return _tcpClient == null;
    }

    public override void Dispose()
    {
        _tcpClient?.Close();
        _tcpListener?.Stop();
    }
}