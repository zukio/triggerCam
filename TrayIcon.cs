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
        }        private void Initialize()
        {
            uiContext = SynchronizationContext.Current;
            settings = AppSettings.Instance;

            context.Opened += ContextOpened;
            context.Closing += ContextClosing;
            context.Closed += (s, e) => { isContextMenuOpen = false; };            contextMenu_comPort.SelectedIndexChanged += OnSettingChanged;
            contextMenu_baudRate.SelectedIndexChanged += OnSettingChanged;
            contextMenu_cameraSelect.SelectedIndexChanged += OnSettingChanged;
            contextMenu_mode.SelectedIndexChanged += OnModeChanged;
            contextMenu_address.TextChanged += OnAddressChanged;
            contextMenu_recordingsPath.PathChanged += OnRecordingsDirChanged;
            contextMenu_imageFormat.SelectedIndexChanged += OnSettingChanged;
            contextMenu_codec.SelectedIndexChanged += OnSettingChanged;

            LoadSettings();
            
            // 初期状態でのモードに応じたボタン表示の更新
            UpdateButtonVisibility();
        }        private void LoadSettings()
        {
            if (settings == null) return;
            
            contextMenu_comPort.Items.Clear();
            contextMenu_comPort.Items.AddRange(SerialPort.GetPortNames());
            contextMenu_comPort.Text = settings.ComPort;
            contextMenu_baudRate.Text = settings.BaudRate.ToString();
            contextMenu_cameraSelect.Text = settings.CameraIndex.ToString();
            contextMenu_mode.SelectedIndex = 1; // デフォルトは動画モード
            contextMenu_recordingsPath.Path = settings.CameraSaveDirectory;
            contextMenu_address.Text = settings.UdpToAddress;
            
            // モードに応じたフォーマット設定の読み込みはUpdateButtonVisibilityで行う
            
            var addressParts = settings.UdpToAddress.Split(':');
            if (addressParts.Length == 2 && int.TryParse(addressParts[1], out int port))
            {
                udpToIP = addressParts[0];
                udpToPort = port;
            }
            
            // モードに応じた表示更新
            UpdateButtonVisibility();
        }        private void LoadImageFormatSetting()
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
              // モードに応じた設定の保存
            if (contextMenu_mode.SelectedIndex == 0) // 静止画モード
            {
                // 画像形式の保存
                settings.ImageFormat = contextMenu_imageFormat.Text.ToLower();
            }
            else // 動画モード
            {
                // ビデオコーデックの保存
                settings.VideoCodec = contextMenu_codec.Text.ToUpper();
            }
            
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

        /// <summary>
        /// 外部から設定が変更された場合にUIを更新
        /// </summary>
        public void UpdateSettings()
        {
            if (uiContext == null || settings == null) return;

            // UIスレッドで実行
            uiContext.Post(_ => 
            {
                // 設定を再読み込み
                LoadSettings();
                
                // 保存ボタンを無効化（変更なし状態）
                contextMenu_save.Enabled = false;
            }, null);
        }

        /// <summary>
        /// カメラ録画の状態を更新
        /// </summary>
        /// <param name="isRec">録画中かどうか</param>
        public void UpdateRecordingState(bool isRec)
        {
            if (uiContext == null) return;

            // UIスレッドで実行
            uiContext.Post(_ => 
            {
                isRecording = isRec;
                
                // タスクトレイアイコンの状態を更新
                if (isRecording)
                {
                    notifyIcon1.Text = "録画中...";
                    // アイコンを録画中のものに変更する場合はここで
                }
                else
                {
                    notifyIcon1.Text = "待機中";
                    // アイコンを通常のものに戻す場合はここで
                }
                
                // ボタン表示も更新
                UpdateButtonVisibility();
            }, null);
        }        /// <summary>
        /// モード変更時の処理
        /// </summary>
        private void OnModeChanged(object? sender, EventArgs e)
        {
            // 保存ボタンを有効化
            contextMenu_save.Enabled = true;
            
            // モードに応じてUIを更新
            UpdateButtonVisibility();
            
            // モード変更を通知
            Console.WriteLine($"モード変更: {(contextMenu_mode.SelectedIndex == 0 ? "静止画" : "動画")}");
        }
          /// <summary>
        /// 選択されたモード（静止画/動画）に応じてUIの表示/非表示を切り替える
        /// </summary>
        private void UpdateButtonVisibility()
        {
            // モードインデックス: 0=静止画、1=動画
            bool isImageMode = contextMenu_mode.SelectedIndex == 0;
              // ボタンの表示切替
            contextMenu_triggerSnap.Visible = isImageMode;
            contextMenu_triggerStart.Visible = !isImageMode;
            contextMenu_triggerStop.Visible = !isImageMode && isRecording;
              // コーデックコントロールの表示/非表示を切り替え
            contextMenu_codec.Visible = !isImageMode;
            contextMenu_codecLabel.Visible = !isImageMode;
            
            // 画像フォーマットコントロールの表示設定
            contextMenu_imageFormat.Visible = isImageMode;
            contextMenu_imageFormatLabel.Visible = isImageMode;
            
            // フォーマット設定の表示を調整
            if (isImageMode)
            {
                // 静止画モードの場合は画像フォーマットを設定
                contextMenu_imageFormat.Items.Clear();
                contextMenu_imageFormat.Items.AddRange(new object[] { "PNG", "JPG", "BMP" });
                
                // 現在の設定を選択
                if (settings != null)
                {
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
                    if (!found && contextMenu_imageFormat.Items.Count > 0)
                    {
                        contextMenu_imageFormat.Text = contextMenu_imageFormat.Items[0].ToString();
                    }
                }
            }
            else
            {
                // 動画モードの場合はコーデックを設定
                contextMenu_codec.Items.Clear();
                contextMenu_codec.Items.AddRange(new object[] { "H264", "MJPG", "MP4V" });
                
                // 現在の設定を選択
                if (settings != null)
                {
                    string codec = settings.VideoCodec.ToUpper();
                    bool found = false;
                    foreach (var item in contextMenu_codec.Items)
                    {
                        if (item.ToString() == codec)
                        {
                            contextMenu_codec.Text = codec;
                            found = true;
                            break;
                        }
                    }
                    if (!found && contextMenu_codec.Items.Count > 0)
                    {
                        contextMenu_codec.Text = contextMenu_codec.Items[0].ToString();
                    }
                }
            }
            
            // カメラの現在の設定を取得してステータス表示を更新
            var cameraRecorder = Program.GetCameraRecorder();
            string cameraInfo = "";
            if (cameraRecorder != null)
            {
                var settings = cameraRecorder.GetSettings();
                if (settings.ContainsKey("resolution") && settings.ContainsKey("frameRate"))
                {
                    cameraInfo = $" ({settings["resolution"]}, {settings["frameRate"]}fps)";
                }
            }
            
            // 録画状態ラベルの更新
            if (isImageMode)
            {
                contextMenu_recordingStatus.Text = "モード: 静止画撮影" + cameraInfo;
            }
            else
            {
                contextMenu_recordingStatus.Text = (isRecording ? "録画状態: 録画中" : "録画状態: 停止中") + cameraInfo;
            }
        }
        
        /// <summary>
        /// SNAPボタンクリック時の処理
        /// </summary>
        private void contextMenu_triggerSnap_Click(object? sender, EventArgs e)
        {
            // CameraRecorderへのアクセス方法はプログラム構造に合わせて調整が必要
            var cameraRecorder = Program.GetCameraRecorder();
            if (cameraRecorder != null)
            {
                string fileName = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                cameraRecorder.TakeSnapshot(fileName);
            }
        }
        
        /// <summary>
        /// STARTボタンクリック時の処理
        /// </summary>
        private void contextMenu_triggerStart_Click(object? sender, EventArgs e)
        {
            // CameraRecorderへのアクセス方法はプログラム構造に合わせて調整が必要
            var cameraRecorder = Program.GetCameraRecorder();
            if (cameraRecorder != null && !isRecording)
            {
                string fileName = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                cameraRecorder.StartRecording(fileName);
                isRecording = true;
                UpdateRecordingState(true);
            }
        }
        
        /// <summary>
        /// STOPボタンクリック時の処理
        /// </summary>
        private void contextMenu_triggerStop_Click(object? sender, EventArgs e)
        {
            // CameraRecorderへのアクセス方法はプログラム構造に合わせて調整が必要
            var cameraRecorder = Program.GetCameraRecorder();
            if (cameraRecorder != null && isRecording)
            {
                cameraRecorder.StopRecording();
                isRecording = false;
                UpdateRecordingState(false);
            }
        }

        /// <summary>
        /// 現在選択されているモードを取得 (0:静止画, 1:動画)
        /// </summary>
        /// <returns>選択されているモードのインデックス</returns>
        public int GetSelectedMode()
        {
            return contextMenu_mode.SelectedIndex;
        }

        /// <summary>
        /// 外部からモードを設定
        /// </summary>
        /// <param name="modeIndex">設定するモード (0:静止画, 1:動画)</param>
        public void SetMode(int modeIndex)
        {
            if (uiContext == null) return;
            
            // UIスレッドで実行
            uiContext.Post(_ => 
            {
                if (modeIndex >= 0 && modeIndex < contextMenu_mode.Items.Count)
                {
                    contextMenu_mode.SelectedIndex = modeIndex;
                    // OnModeChangedイベントが自動的に呼ばれる
                }
            }, null);
        }
    }
}
