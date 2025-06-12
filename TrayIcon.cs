﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using triggerCam.UDP;
using triggerCam.Audio;
using triggerCam.Settings;

namespace triggerCam
{
    public partial class TrayIcon : Component
    {
        // Mainスレッドと処理を分けるためにUIスレッドを取得
        SynchronizationContext? uiContext;

        public string deviceName { get; private set; } = "";
        public string udpAddress { get; private set; } = "127.0.0.1:10000";
        public static bool isContextMenuOpen { get; private set; } = false;
        
        // マイクと録音の状態
        private bool isMicMuted = false;
        private bool isRecording = false;
        private string currentRecordingFile = "";
        private bool useTempFile = true;
        
        // 閾値設定
        private int silenceThreshold = 150;
        private int minAudioSamples = 10;
        
        // 録音機能
        private AudioRecorder? audioRecorder;
        
        // 設定
        private AppSettings settings;

        // UDP送信先
        private string udpToIP = "127.0.0.1";
        private int udpToPort = 10000;

        // イベント：ユーザーが設定を変更＆保存＆再起動
        public event Action<string>? reStart;
        public event Action<string>? changeUdpAddress;
        
        // イベント：マイクと録音の操作
        public event Action<bool>? toggleMic;
        public event Action<string?>? startRecording;
        public event Action<bool>? stopRecording;
        
        
        // イベント：閾値設定の変更
        public event Action<int, int>? thresholdChanged;
        // イベントハンドラを名前付きデリゲートとして定義
        ToolStripDropDownClosedEventHandler closedHandler = (sender, e) => { isContextMenuOpen = false; };

        public TrayIcon()
        {
            InitializeComponent();
            thenInitialized();
        }

        public TrayIcon(IContainer container)
        {
            container.Add(this);
            InitializeComponent();
            thenInitialized();
        }

        void thenInitialized()
        {
            uiContext = SynchronizationContext.Current;
            settings = AppSettings.Instance;
            
            // イベントハンドラ
            context.Opened += contextMenu_open;
            context.Closing += contextMenu_closing;
            context.Closed += closedHandler;
            contextMenu_select.SelectedIndexChanged += new EventHandler(ComboBox_SelectedIndexChanged);
            contextMenu_address.TextChanged += OnAddressChanged;
            contextMenu_recordingsPath.PathChanged += OnRecordingsDirChanged;

            // 一時ファイル設定の初期化
             useTempFile = settings.UseTempFile;
             // contextMenu_tempFileSettingsが初期化されていることを確認
             if (contextMenu_tempFileSettings != null)
             {
                 contextMenu_tempFileSettings.SettingsChanged += OnTempFileSettingsChanged;
             }
             else
             {
                 Console.WriteLine("Warning: contextMenu_tempFileSettings is null");
             }
             // contextMenu_tempFileSettingsが初期化されていることを確認
             if (contextMenu_tempFileSettings != null)
             {
                 contextMenu_tempFileSettings.UseTempFile = useTempFile;
                 
                 // 一時ファイル使用時は録音データ関連のメニューを無効化
                 UpdateRecordingMenuState(useTempFile);
             }
             else
             {
                 Console.WriteLine("Warning: contextMenu_tempFileSettings is null during initialization");
             }
            
            LoadSettings();
        }
        
        private void LoadSettings()
        {
            settings = AppSettings.Instance;
            silenceThreshold = settings.SilenceThreshold;
            minAudioSamples = settings.MinAudioSamples;
            useTempFile = settings.UseTempFile;
            
            contextMenu_threshold.Value = silenceThreshold;
            contextMenu_recordingsPath.Path = settings.RecordingsDirectory;
            if (contextMenu_tempFileSettings != null)
             {
                 contextMenu_tempFileSettings.UseTempFile = useTempFile;
                 UpdateRecordingMenuState(useTempFile);
             }
 

            // UDP設定を解析
            var parts = settings.UdpToAddress.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[1], out int port))
            {
                udpToIP = parts[0];
                udpToPort = port;
            }
        }

        /// <summary>
        /// 一時ファイル設定が変更された時の処理
        /// </summary>
         private void OnTempFileSettingsChanged(object? sender, EventArgs e)
         {
             bool newUseTempFile = contextMenu_tempFileSettings.UseTempFile;
             
             // 設定が変更された場合のみ保存ボタンを有効化
             if (newUseTempFile != useTempFile)
             {
                 contextMenu_save.Enabled = true;
                 
                 // 録音データ関連のメニューの有効/無効を切り替え
                 UpdateRecordingMenuState(newUseTempFile);
                 
                 // 変更をすぐに反映
                 useTempFile = newUseTempFile;
             }
         }
         
         /// <summary>
         /// 録音データ関連のメニューの有効/無効を切り替える
         /// </summary>
         /// <param name="useTempFile">一時ファイルを使用するかどうか</param>
         private void UpdateRecordingMenuState(bool useTempFile)
         {
             // 一時ファイル使用時は録音データ関連のメニューを無効化
             if (contextMenu_recordingsDirLabel != null)
                 contextMenu_recordingsDirLabel.Enabled = !useTempFile;
                 
             if (contextMenu_recordingsPath != null)
                 contextMenu_recordingsPath.Enabled = !useTempFile;
                 
             if (contextMenu_openRecordingsDir != null)
                 contextMenu_openRecordingsDir.Enabled = !useTempFile;
         }
         
        
        public void SetAudioRecorder(AudioRecorder recorder)
        {
            audioRecorder = recorder;
        }

        public new void Dispose()
        {
            uiContext = null;
            reStart = null;
            context.Opened -= contextMenu_open;
            context.Closed -= closedHandler;
            contextMenu_select.SelectedIndexChanged -= ComboBox_SelectedIndexChanged;
            contextMenu_address.TextChanged -= OnAddressChanged;
						contextMenu_recordingsPath.PathChanged -= OnRecordingsDirChanged;
            if (contextMenu_tempFileSettings != null)
                 contextMenu_tempFileSettings.SettingsChanged -= OnTempFileSettingsChanged;
            thresholdChanged = null;
            base.Dispose();
        }

        void contextMenu_exit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void trayIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (isContextMenuOpen) context.Close();
                else context.Show(Cursor.Position);
            }
        }

        private void contextMenu_open(object? sender, EventArgs e)
        {
            isContextMenuOpen = true;

            if (contextMenu_save.Enabled)
            {
                contextMenu_select.Text = deviceName;
                contextMenu_address.Text = udpAddress;
                contextMenu_save.Enabled = false;
            }
            
            contextMenu_threshold.Value = silenceThreshold;
            UpdateMicStateUI(isMicMuted);
        }

        private void ComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            string? nextDevice = contextMenu_select.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(nextDevice) && !nextDevice.Contains(deviceName))
            {
                contextMenu_save.Enabled = true;
            }
            else if (contextMenu_save.Enabled)
            {
                contextMenu_save.Enabled = false;
            }
        }
        

        private void OnAddressChanged(object? sender, EventArgs e)
        {
            string nextAdress = contextMenu_address.Text;
            if (!string.Equals(nextAdress, udpAddress))
            {
                contextMenu_save.Enabled = true;
            }
            else if (contextMenu_save.Enabled && string.Equals(contextMenu_recordingsPath.Path, settings.RecordingsDirectory))
            {
                contextMenu_save.Enabled = false;
            }
        }
        
        private void OnRecordingsDirChanged(object? sender, EventArgs e)
        {
            string nextDir = contextMenu_recordingsPath.Path;
            if (!string.Equals(nextDir, settings.RecordingsDirectory))
            {
                contextMenu_save.Enabled = true;
            }
            else if (contextMenu_save.Enabled && string.Equals(contextMenu_address.Text, udpAddress))
            {
                contextMenu_save.Enabled = false;
            }
        }

        void contextMenu_save_Click(object sender, EventArgs e)
        {
            string nextDevice = (contextMenu_select.SelectedItem != null) ? contextMenu_select.SelectedItem.ToString() ?? deviceName : deviceName;
            if (!string.Equals(nextDevice, deviceName))
            {
                Console.WriteLine($"User Change Device: {nextDevice}");
                reStart?.Invoke(nextDevice);
                settings.DeviceName = nextDevice;
                settings.Save();
            }

            string nextAdress = udpAddress;
            if (!string.Equals(contextMenu_address.Text, udpAddress))
            {
                var udpAddressChanger = new UdpAddressChanger(contextMenu_address.Text);
                if (udpAddressChanger.isValid)
                {
                    nextAdress = $"{udpAddressChanger.ip}:{udpAddressChanger.port}";
                    Console.WriteLine($"User Change Address: {nextAdress}");
                    changeUdpAddress?.Invoke(nextAdress);
                    settings.UdpToAddress = nextAdress;
                    settings.Save();
                }
            }
            
            string nextDir = contextMenu_recordingsPath.Path;
            if (!string.Equals(nextDir, settings.RecordingsDirectory))
            {
                Console.WriteLine($"User Change Recordings Directory: {nextDir}");
                settings.RecordingsDirectory = nextDir;
                settings.Save();
            }

            // 一時ファイル設定の保存
            bool newUseTempFile = contextMenu_tempFileSettings.UseTempFile;
            if (newUseTempFile != useTempFile)
            {
                Console.WriteLine($"User Change Temp File Settings: UseTempFile={newUseTempFile}");
                useTempFile = newUseTempFile;
                
                settings.UseTempFile = useTempFile;
                settings.Save();
                
                // AudioRecorderの設定も更新
                audioRecorder?.UpdateTempFileSettings(useTempFile);
            }
            
            
            setDevice(nextDevice, nextAdress);
        }

        public void setDevice(string selectedDevice, string ipPort = "127.0.0.1:10000")
        {
            deviceName = selectedDevice;
            contextMenu_select.Text = selectedDevice;
            udpAddress = ipPort;
            contextMenu_address.Text = ipPort;
            contextMenu_save.Enabled = false;

            var parts = ipPort.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[1], out int port))
            {
                udpToIP = parts[0];
                udpToPort = port;
            }
        }

        public void contextMenu_state_connect(string? connectedName)
        {
            uiContext?.Post(new SendOrPostCallback((o) =>
            {
                try
                {
                    contextMenu_state.Text = (connectedName != null) ? "State: Connected" : "State: Lost";
                    Console.WriteLine($"UI updated - Connection state: {(connectedName != null ? "Connected" : "Lost")}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating connection state UI: {ex.Message}");
                }
            }), null);
        }

        public void contextMenu_state_mute(bool isMuted)
        {
            isMicMuted = isMuted;
            UpdateMicStateUI(isMuted);
            SendStateNotification(isMuted ? "isMute" : "ON");
        }
        
        private void UpdateMicStateUI(bool isMuted)
        {
            uiContext?.Post(new SendOrPostCallback((o) =>
            {
                try
                {
                    contextMenu_state.Text = (isMuted) ? "State: Mute" : "State: ON";
                    contextMenu_micToggle.Text = (isMuted) ? "マイクON" : "マイクMute";
                    Console.WriteLine($"UI updated - Mic state: {(isMuted ? "Mute" : "ON")}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating mic state UI: {ex.Message}");
                }
            }), null);
        }
        
        private void contextMenu_micToggle_Click(object sender, EventArgs e)
        {
            toggleMic?.Invoke(!isMicMuted);
            SendStateNotification(!isMicMuted ? "isMute" : "ON");
        }
        
        private void contextMenu_browseRecordingsDir_Click(object sender, EventArgs e)
        {
            try
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    dialog.InitialDirectory = contextMenu_recordingsPath.Path;
                    dialog.Description = "録音ファイルの保存場所を選択してください";
                    dialog.UseDescriptionForTitle = true;
                    
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        contextMenu_recordingsPath.Path = dialog.SelectedPath;
                        Console.WriteLine($"Selected recordings directory: {dialog.SelectedPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error browsing for recordings directory: {ex.Message}");
                MessageBox.Show($"フォルダを選択できませんでした：{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void contextMenu_openRecordingsDir_Click(object sender, EventArgs e)
        {
            try
            {
                string path = contextMenu_recordingsPath.Path;
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                Process.Start("explorer.exe", $"\"{path}\"");
                Console.WriteLine($"Opening recordings directory: {path}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening recordings directory: {ex.Message}");
                MessageBox.Show($"保存場所を開けませんでした：{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void contextMenu_closing(object? sender, ToolStripDropDownClosingEventArgs e)
        {
            if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked &&
                context.Items.Cast<ToolStripItem>().LastOrDefault(i => i.Selected) == contextMenu_recToggle)
            {
                e.Cancel = true;
            }
        }

        private void contextMenu_recToggle_Click(object sender, EventArgs e)
        {
            if (isRecording)
            {
                // 録音停止時、一時ファイルを使用している場合は保存するかどうかを確認
                 if (useTempFile)
                 {
                    // var result = MessageBox.Show(
                    //     "録音データをファイルに保存しますか？",
                    //     "録音停止",
                    //     MessageBoxButtons.YesNo,
                    //     MessageBoxIcon.Question);
                     
                    // bool saveToFile = (result == DialogResult.Yes);
                     stopRecording?.Invoke(false);
                 }
                 else
                 {
                     stopRecording?.Invoke(false);
                 }
            }
            else
            {
                string fileName = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                startRecording?.Invoke(fileName);
                SendStateNotification("RecStart");
            }
        }
        
        public void UpdateRecordingState(bool recording, string filePath = "")
        {
            isRecording = recording;
            currentRecordingFile = filePath;
            
            // 録音停止時のみファイルパスを含めて通知
            if (!recording)
            {
                SendStateNotification("RecStop", filePath);
            }
            
            uiContext?.Post(new SendOrPostCallback((o) =>
            {
                try
                {
                    contextMenu_recToggle.Text = recording ? "録音終了" : "録音する";
                    Console.WriteLine($"UI updated - Recording state: {(recording ? "Recording" : "Stopped")}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating recording state UI: {ex.Message}");
                }
            }), null);
        }

        /// <summary>
        /// 一時ファイル設定を更新する
        /// </summary>
        /// <param name="useTempFile">一時ファイルを使用するかどうか</param>
        public void UpdateTempFileState(bool useTempFile)
        {
             this.useTempFile = useTempFile;
             
             uiContext?.Post(new SendOrPostCallback((o) =>
             {
                 try
                 {
                     if (contextMenu_tempFileSettings != null)
                     {
                         contextMenu_tempFileSettings.UseTempFile = useTempFile;
                         UpdateRecordingMenuState(useTempFile);
                     }
                     Console.WriteLine($"UI updated - Temp file state: {(useTempFile ? "Using temp file" : "Using permanent file")}");
                 }
                 catch (Exception ex)
                 {
                     Console.WriteLine($"Error updating temp file state UI: {ex.Message}");
                 }
            }), null);
        }
         
        
        private void contextMenu_threshold_ValueChanged(object sender, EventArgs e)
        {
            silenceThreshold = contextMenu_threshold.Value;
            thresholdChanged?.Invoke(silenceThreshold, minAudioSamples);
            audioRecorder?.UpdateThresholdSettings(silenceThreshold, minAudioSamples);
            contextMenu_save.Enabled = true;
        }

        public void contextMenu_select_update(string[] detected_devices)
        {
            uiContext?.Post(new SendOrPostCallback((o) =>
            {
                try
                {
                    contextMenu_select.Items.Clear();
                    contextMenu_select.Items.AddRange(detected_devices);
                    Console.WriteLine($"UI updated - Device list: {detected_devices.Length} devices");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating device list UI: {ex.Message}");
                }
            }), null);
        }

        private void SendStateNotification(string state, string? filePath = null)
        {
            try
            {
                // 録音停止時はファイルパスを含める
                if (state == "RecStop")
                {
                    UDPSender.SendUDP($"RecStop {filePath ?? "null"}", udpToIP, udpToPort);
                }
                else
                {
                    UDPSender.SendUDP(state, udpToIP, udpToPort);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending state notification: {ex.Message}");
            }
        }
    }
}
