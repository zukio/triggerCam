using System.Collections.Generic;
using System.Text.Json;
using System.Net.Sockets;

namespace triggerCam.UDP
{
    internal static class UdpNotifier
    {
        public static void SendStatus(UdpClient client, string ip, int port, string status, string message, Dictionary<string, object>? data = null)
        {
            var payload = new ResponseData
            {
                status = status,
                message = message,
                data = data
            };
            string json = JsonSerializer.Serialize(payload);
            UDPSender.SendUDP(client, json, ip, port);
        }
    }
}
