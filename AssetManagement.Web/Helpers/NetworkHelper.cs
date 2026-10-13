using System;
using System.Net;
using System.Net.Sockets;

namespace AssetManagement.Web.Helpers;

public static class NetworkHelper
{
    private static string? _cachedLanIp;
    private static DateTime _lastChecked = DateTime.MinValue;

    /// <summary>
    /// Gets the primary local IPv4 address of this machine (e.g. 10.192.168.45).
    /// </summary>
    public static string GetLocalIpAddress()
    {
        if (!string.IsNullOrEmpty(_cachedLanIp) && (DateTime.UtcNow - _lastChecked).TotalMinutes < 5)
        {
            return _cachedLanIp;
        }

        try
        {
            // UDP socket probe to detect the active outbound network interface without transmitting packets
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            if (socket.LocalEndPoint is IPEndPoint endPoint && !IPAddress.IsLoopback(endPoint.Address))
            {
                var ip = endPoint.Address.ToString();
                if (!string.IsNullOrWhiteSpace(ip) && !ip.StartsWith("127.") && !ip.StartsWith("169.254."))
                {
                    _cachedLanIp = ip;
                    _lastChecked = DateTime.UtcNow;
                    return _cachedLanIp;
                }
            }
        }
        catch
        {
            // Fall through to Dns check
        }

        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(ip))
                {
                    var ipStr = ip.ToString();
                    if (!ipStr.StartsWith("169.254."))
                    {
                        _cachedLanIp = ipStr;
                        _lastChecked = DateTime.UtcNow;
                        return _cachedLanIp;
                    }
                }
            }
        }
        catch
        {
            // Fall through
        }

        return "localhost";
    }

    /// <summary>
    /// Converts a BaseUri (which might be localhost:5135) to a scannable LAN URI (e.g. http://10.192.168.45:5135/...)
    /// so mobile devices connected to the same Wi-Fi / LAN network can reach the server.
    /// </summary>
    public static string ToScannableUri(string baseUri, string relativePath)
    {
        var cleanRel = string.IsNullOrWhiteSpace(relativePath) ? "" : (relativePath.StartsWith('/') ? relativePath : "/" + relativePath);
        if (string.IsNullOrWhiteSpace(baseUri)) return cleanRel;

        try
        {
            var uri = new Uri(baseUri);
            var host = uri.Host;

            if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                host.Equals("0.0.0.0", StringComparison.OrdinalIgnoreCase))
            {
                var lanIp = GetLocalIpAddress();
                if (!string.Equals(lanIp, "localhost", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(lanIp))
                {
                    var builder = new UriBuilder(uri) { Host = lanIp };
                    return builder.Uri.ToString().TrimEnd('/') + cleanRel;
                }
            }

            return uri.ToString().TrimEnd('/') + cleanRel;
        }
        catch
        {
            return baseUri.TrimEnd('/') + cleanRel;
        }
    }

    /// <summary>
    /// Generates a high-quality QR code image URL from api.qrserver.com
    /// </summary>
    public static string GetQrCodeUrl(string data, int size = 300, int margin = 2)
    {
        if (string.IsNullOrWhiteSpace(data)) return string.Empty;
        return $"https://api.qrserver.com/v1/create-qr-code/?size={size}x{size}&margin={margin}&data={Uri.EscapeDataString(data)}";
    }
}
