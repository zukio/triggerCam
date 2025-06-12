using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using triggerCam.Settings;

namespace triggerCam
{
    public partial class TrayIcon : Component
    {
        SynchronizationContext? uiContext;
        private AppSettings settings;
        private string udpToIP = "127.0.0.1";
        private int udpToPort = 10000;

        public TrayIcon()
        {
            InitializeComponent();
            Initialize();
        }

        public TrayIcon(IContainer container)
        {
            container.Add(this);
            InitializeComponent();
            Initialize();
        }

        private void Initialize()
        {
            uiContext = SynchronizationContext.Current;
            settings = AppSettings.Instance;

            context.Opened += ContextOpened;
            context.Closing += ContextClosing;
            context.Closed += (s, e) => { isContextMenuOpen = false; };

            contextMenu_comPort.SelectedIndexChanged += OnSettingChanged;
            contextMenu_baudRate.SelectedIndexChanged += OnSettingChanged;
            contextMenu_cameraSelect.SelectedIndexChanged += OnSettingChanged;
            contextMenu_mode.SelectedIndexChanged += OnSettingChanged;
            contextMenu_triggerSnap.TextChanged += OnSettingChanged;
            contextMenu_triggerStart.TextChanged += OnSettingChanged;
            contextMenu_triggerStop.TextChanged += OnSettingChanged;
            contextMenu_address.TextChanged += OnAddressChanged;
            contextMenu_recordingsPath.PathChanged += OnRecordingsDirChanged;

            LoadSettings();
        }

        private void LoadSettings()
        {
            contextMenu_comPort.Items.Clear();
            contextMenu_comPort.Items.AddRange(SerialPort.GetPortNames());
            contextMenu_comPort.Text = settings.ComPort;
            contextMenu_baudRate.Text = settings.BaudRate.ToString();
            contextMenu_triggerSnap.Text = "SNAP";
            contextMenu_triggerStart.Text = "START";
            contextMenu_triggerStop.Text = "STOP";
            contextMenu_cameraSelect.Text = settings.CameraIndex.ToString();
            contextMenu_mode.SelectedIndex = 1; // video
            contextMenu_recordingsPath.Path = settings.CameraSaveDirectory;
            contextMenu_address.Text = settings.UdpToAddress;

            var parts = settings.UdpToAddress.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[1], out int port))
            {
                udpToIP = parts[0];
                udpToPort = port;
            }
        }

        public static bool isContextMenuOpen { get; private set; } = false;

        private void ContextOpened(object? sender, EventArgs e)
        {
            isContextMenuOpen = true;
            contextMenu_save.Enabled = false;
        }

        private void ContextClosing(object? sender, ToolStripDropDownClosingEventArgs e)
        {
            // nothing special
        }

        private void trayIcon_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (isContextMenuOpen) context.Close();
                else context.Show(Cursor.Position);
            }
        }

        private void OnSettingChanged(object? sender, EventArgs e)
        {
            contextMenu_save.Enabled = true;
        }

        private void OnAddressChanged(object? sender, EventArgs e)
        {
            if (!string.Equals(contextMenu_address.Text, settings.UdpToAddress))
            {
                contextMenu_save.Enabled = true;
            }
        }

        private void OnRecordingsDirChanged(object? sender, EventArgs e)
        {
            if (!string.Equals(contextMenu_recordingsPath.Path, settings.CameraSaveDirectory))
            {
                contextMenu_save.Enabled = true;
            }
        }

        private void contextMenu_save_Click(object sender, EventArgs e)
        {
            settings.ComPort = contextMenu_comPort.Text;
            if (int.TryParse(contextMenu_baudRate.Text, out int br))
                settings.BaudRate = br;
            if (int.TryParse(contextMenu_cameraSelect.Text, out int camIdx))
                settings.CameraIndex = camIdx;
            settings.CameraSaveDirectory = contextMenu_recordingsPath.Path;
            settings.UdpToAddress = contextMenu_address.Text;
            settings.Save();

            var parts = settings.UdpToAddress.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[1], out int port))
            {
                udpToIP = parts[0];
                udpToPort = port;
            }

            contextMenu_save.Enabled = false;
        }

        private void contextMenu_browseRecordingsDir_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.SelectedPath = contextMenu_recordingsPath.Path;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                contextMenu_recordingsPath.Path = dialog.SelectedPath;
            }
        }

        private void contextMenu_openRecordingsDir_Click(object sender, EventArgs e)
        {
            string path = contextMenu_recordingsPath.Path;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            Process.Start("explorer.exe", $"\"{path}\"");
        }

        private void contextMenu_exit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        // Called from UDP CommandProcessor
        public void contextMenu_state_mute(bool mute)
        {
            // no-op in simplified version
        }

        public void contextMenu_select_update(string[] devices)
        {
            uiContext?.Post(_ =>
            {
                contextMenu_cameraSelect.Items.Clear();
                contextMenu_cameraSelect.Items.AddRange(devices);
            }, null);
        }

        // Placeholder methods for compatibility with CommandProcessor
        public void UpdateRecordingState(bool recording, string filePath = "")
        {
            // no-op in simplified menu
        }

        public void UpdateTempFileState(bool useTempFile)
        {
            // no-op in simplified menu
        }
    }
}
