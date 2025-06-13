using System;
using System.Net;

namespace triggerCam.UDP
{
    internal class UdpAddressChanger
    {
        public string ip { get; private set; } = "127.0.0.1";
        public int port { get; private set; } = 10000;

        public bool isValid { get; private set; } = false;

        public UdpAddressChanger(string newAddress)
        {
            // Split the string by the colon
            string[] parts = newAddress.Split(':');
            if (parts.Length != 2)
            {
                Console.WriteLine("Invalid format. Use IP:Port.");
                global::LogWriter.AddErrorLog("Invalid UDP address format", nameof(UdpAddressChanger));
                return;
            }

            // Validate and set the IP address
            if (IPAddress.TryParse(parts[0], out IPAddress? parsedIP))
            {
                ip = parsedIP.ToString();
            }
            else
            {
                Console.WriteLine("Invalid IP address.");
                global::LogWriter.AddErrorLog("Invalid IP address", nameof(UdpAddressChanger));
                return;
            }

            // Validate and set the port
            if (int.TryParse(parts[1], out int parsedPort))
            {
                if (parsedPort >= 0 && parsedPort <= 65535)
                {
                    port = parsedPort;
                }
                else
                {
                    Console.WriteLine("Invalid port number. It should be between 0 and 65535.");
                    global::LogWriter.AddErrorLog("Invalid port number", nameof(UdpAddressChanger));
                    return;
                }
            }
            else
            {
                Console.WriteLine("Invalid port number.");
                global::LogWriter.AddErrorLog("Invalid port number", nameof(UdpAddressChanger));
                return;
            }

            // If reached here, ip and port are successfully updated.
            Console.WriteLine($"New UDP Address: {ip}:{port}");
            isValid = true;
        }
    }
}
