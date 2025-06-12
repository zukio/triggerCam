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
        private AppSettings? settings;
        private string udpToIP = "127.0.0.1";
        private int udpToPort = 10000;
        private bool isRecording = false;
        private string lastRecordedFile = "";

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
            contextMenu_resolution.SelectedIndexChanged += OnSettingChanged;
            contextMenu_frameRate.SelectedIndexChanged += OnSettingChanged;
            contextMenu_imageFormat.SelectedIndexChanged += OnSettingChanged;

            LoadSettings();
        }

        private void LoadSettings()
        {
            if (settings == null) return;
            
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

            // カメラ設定の読み込み
            LoadResolutionSetting();
            LoadFrameRateSetting();
            LoadImageFormatSetting();

            var addressParts = settings.UdpToAddress.Split(':');
            if (addressParts.Length == 2 && int.TryParse(addressParts[1], out int port))
            {
                udpToIP = addressParts[0];
                udpToPort = port;
            }
        }

        private void LoadResolutionSetting()
        {
            if (settings == null) return;
            
            string resolution = $"{settings.VideoWidth}x{settings.VideoHeight}";
            bool found = false;

            foreach (var item in contextMenu_resolution.Items)
            {
                if (item.ToString() == resolution)
                {
                    contextMenu_resolution.Text = resolution;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                contextMenu_resolution.Items.Add(resolution);
                contextMenu_resolution.Text = resolution;
            }
        }

        private void LoadFrameRateSetting()
        {
            if (settings == null) return;
            
            string frameRate = settings.FrameRate.ToString();
            bool found = false;

            foreach (var item in contextMenu_frameRate.Items)
            {
                if (item.ToString() == frameRate)
                {
                    contextMenu_frameRate.Text = frameRate;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                contextMenu_frameRate.Items.Add(frameRate);
                contextMenu_frameRate.Text = frameRate;
            }
        }

        private void LoadImageFormatSetting()
        {
            if (settings == null) return;
            
            string format = settings.ImageFormat.ToUpper();
            bool found = false;

            foreach (var item in contextMenu_imageFormat.Items)
            {
                if (item.ToString() == format)
                {
                    contextMenu_imageFormat.Text = format;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                contextMenu_imageFormat.Items.Add(format);
                contextMenu_imageFormat.Text = format;
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
            if (settings != null && !string.Equals(contextMenu_address.Text, settings.UdpToAddress))
            {
                contextMenu_save.Enabled = true;
            }
        }

        private void OnRecordingsDirChanged(object? sender, EventArgs e)
        {
            if (settings != null && !string.Equals(contextMenu_recordingsPath.Path, settings.CameraSaveDirectory))
            {
                contextMenu_save.Enabled = true;
            }
        }

        private void contextMenu_save_Click(object sender, EventArgs e)
        {
            if (settings == null) return;
            
            settings.ComPort = contextMenu_comPort.Text;
            if (int.TryParse(contextMenu_baudRate.Text, out int br))
                settings.BaudRate = br;
            if (int.TryParse(contextMenu_cameraSelect.Text, out int camIdx))
                settings.CameraIndex = camIdx;
            settings.CameraSaveDirectory = contextMenu_recordingsPath.Path;
            settings.UdpToAddress = contextMenu_address.Text;
            
            // 解像度設定の保存
            string resolution = contextMenu_resolution.Text;
            if (!string.IsNullOrEmpty(resolution) && resolution.Contains('x'))
            {
                string[] resolutionParts = resolution.Split('x');
                if (resolutionParts.Length == 2)
                {
                    if (int.TryParse(resolutionParts[0], out int width))
                        settings.VideoWidth = width;
                    if (int.TryParse(resolutionParts[1], out int height))
                        settings.VideoHeight = height;
                }
            }
            
            // フレームレート設定の保存
            if (int.TryParse(contextMenu_frameRate.Text, out int fps))
                settings.FrameRate = fps;
            
            // 画像形式設定の保存
            settings.ImageFormat = contextMenu_imageFormat.Text.ToLower();
            
            settings.Save();

            var addressParts = settings.UdpToAddress.Split(':');
            if (addressParts.Length == 2 && int.TryParse(addressParts[1], out int port))
            {
                udpToIP = addressParts[0];
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
            // no-op in camera version
        }

        public void contextMenu_select_update(string[] devices)
        {
            uiContext?.Post(_ =>
            {
                contextMenu_cameraSelect.Items.Clear();
                contextMenu_cameraSelect.Items.AddRange(devices);
            }, null);
        }

        // Methods for compatibility with CommandProcessor
        public void UpdateRecordingState(bool recording, string filePath = "")
        {
            isRecording = recording;
            if (!string.IsNullOrEmpty(filePath))
            {
                lastRecordedFile = filePath;
            }

            uiContext?.Post(_ =>
            {
                if (recording)
                {
                    contextMenu_recordingStatus.Text = "録画状態: 録画中";
                    notifyIcon1.Text = "Camera Recorder (Recording)";
                }
                else
                {
                    if (!string.IsNullOrEmpty(lastRecordedFile))
                    {
                        contextMenu_recordingStatus.Text = $"録画状態: 停止 (最終: {Path.GetFileName(lastRecordedFile)})";
                    }
                    else
                    {
                        contextMenu_recordingStatus.Text = "録画状態: 停止中";
                    }
                    notifyIcon1.Text = "Camera Recorder";
                }
            }, null);
        }

        public void UpdateTempFileState(bool useTempFile)
        {
            // no-op in camera version
        }
    }
}
