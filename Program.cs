using System;
using System.Net.Sockets;
using System.Windows.Forms;
using micNotifyUDP.Camera;
using micNotifyUDP.Settings;

namespace micNotifyUDP
{
    internal static class Program
    {
        private static SerialTriggerListener? serialListener;
        private static CameraRecorder? cameraRecorder;
        private static UdpClient? udpClient;
        private static string udpToIP = "127.0.0.1";
        private static int udpToPort = 10000;

        [STAThread]
        static void Main()
        {
            var settings = AppSettings.Instance;
            ParseUdpAddress(settings.UdpToAddress, out udpToIP, out udpToPort);
            udpClient = new UdpClient();
            cameraRecorder = new CameraRecorder(settings.CameraIndex, settings.CameraSaveDirectory);
            cameraRecorder.SnapshotSaved += path => SendUdp($"{{ \"type\": \"snapshot\", \"path\": \"{path}\" }}");
            cameraRecorder.VideoSaved += path => SendUdp($"{{ \"type\": \"video_saved\", \"path\": \"{path}\" }}");

            serialListener = new SerialTriggerListener(settings.ComPort, settings.BaudRate);
            serialListener.SnapReceived += () => cameraRecorder!.TakeSnapshot(CreateFileName());
            serialListener.StartReceived += () => cameraRecorder!.StartRecording(CreateFileName());
            serialListener.StopReceived += () => cameraRecorder!.StopRecording();

            serialListener.Start();

            ApplicationConfiguration.Initialize();
            using NotifyIcon notifyIcon = new NotifyIcon
            {
                Text = "Serial Camera Recorder",
                Icon = System.Drawing.SystemIcons.Application,
                Visible = true,
                ContextMenuStrip = new ContextMenuStrip()
            };
            notifyIcon.ContextMenuStrip.Items.Add("Exit", null, (s, e) => Application.Exit());

            Application.ApplicationExit += (s, e) =>
            {
                serialListener?.Dispose();
                cameraRecorder?.Dispose();
                udpClient?.Dispose();
            };

            Application.Run();
        }

        private static void SendUdp(string message)
        {
            if (udpClient != null)
            {
                UDPSender.SendUDP(udpClient, message, udpToIP, udpToPort);
            }
        }

        private static void ParseUdpAddress(string address, out string ip, out int port)
        {
            ip = "127.0.0.1";
            port = 10000;
            if (!string.IsNullOrEmpty(address))
            {
                var parts = address.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[1], out int p))
                {
                    ip = parts[0];
                    port = p;
                }
            }
        }

        private static string CreateFileName() => DateTime.Now.ToString("yyyyMMdd_HHmmss");
    }
}
