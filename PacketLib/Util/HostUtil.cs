using System.Net;

namespace PacketLib.Util;

public static class HostUtil
{
    /// <summary>
    /// Parse an IP address from a string. If the ip is not ipv4 or ipv6, it will be looked up through dns.
    /// </summary>
    /// <param name="host">The ip address to parse.</param>
    /// <returns>An IPAddress parsed from the host.</returns>
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

    /// <summary>
    /// Parse a string containing an IP address and a port.
    /// </summary>
    /// <param name="host">The ip address and port to parse. (ip:port)</param>
    /// <returns>An IPEndPoint parsed from the ip and port in host.</returns>
    /// <exception cref="FormatException">If the port number isn't a number.</exception>
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