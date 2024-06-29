using System.Net;

namespace PacketLib.Util;

public static class HostUtil
{
    public static IPAddress ParseIpAddress(string host)
    {
        if (IPAddress.TryParse(host, out IPAddress? ip))
        {
            return ip;
        }
        
        // Get ip from dns
        var hostEntry = Dns.GetHostEntry(host);
        // Use first host
        return hostEntry.AddressList[0];
    }

    public static IPEndPoint ParseIpPort(string host)
    {
        var lastColon  = host.LastIndexOf(':');
        var ip = ParseIpAddress(host.Substring(0, lastColon));
        var port = host.Substring(lastColon + 1);

        if (!int.TryParse(port, out int portInt))
            throw new FormatException("Invalid port number");
        
        return new IPEndPoint(ip, portInt);
    }
}