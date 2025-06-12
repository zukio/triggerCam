using System;
using System.IO.Ports;
using System.Threading;

namespace triggerCam
{
    /// <summary>
    /// シリアルポートからトリガー文字列を受信するクラス
    /// </summary>
    public class SerialTriggerListener : IDisposable
    {
        private readonly SerialPort port;
        private readonly string snapCommand;
        private readonly string startCommand;
        private readonly string stopCommand;

        public event Action? SnapReceived;
        public event Action? StartReceived;
        public event Action? StopReceived;

        public SerialTriggerListener(string portName, int baudRate = 9600, string snapCommand = "SNAP", string startCommand = "START", string stopCommand = "STOP")
        {
            this.snapCommand = snapCommand;
            this.startCommand = startCommand;
            this.stopCommand = stopCommand;
            port = new SerialPort(portName, baudRate)
            {
                NewLine = "\n"
            };
        }

        public void Start()
        {
            port.DataReceived += OnDataReceived;
            port.Open();
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string line = port.ReadLine().Trim();
                if (line.Equals(snapCommand, StringComparison.OrdinalIgnoreCase))
                    SnapReceived?.Invoke();
                else if (line.Equals(startCommand, StringComparison.OrdinalIgnoreCase))
                    StartReceived?.Invoke();
                else if (line.Equals(stopCommand, StringComparison.OrdinalIgnoreCase))
                    StopReceived?.Invoke();
            }
            catch { }
        }

        public void Dispose()
        {
            if (port.IsOpen) port.Close();
            port.Dispose();
        }
    }
}
