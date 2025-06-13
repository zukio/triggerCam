using System.Text.Json;

namespace triggerCam.Settings
{
    /// <summary>
    /// アプリケーション設定を管理するクラス
    /// </summary>
    public class AppSettings
    {
        private static readonly string SettingsFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        // シングルトンインスタンス
        private static AppSettings? _instance;
        public static AppSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Load();
                }
                return _instance;
            }
        }

        // 基本設定項目
        /// <summary>
        /// UDP送信先アドレス
        /// </summary>
        public string UdpToAddress { get; set; } = "127.0.0.1:10000";
        
        /// <summary>
        /// シリアルポート名
        /// </summary>
        public string ComPort { get; set; } = "COM1";
        
        /// <summary>
        /// シリアル通信のボーレート
        /// </summary>
        public int BaudRate { get; set; } = 9600;
        
        /// <summary>
        /// 使用するカメラのインデックス
        /// </summary>
        public int CameraIndex { get; set; } = 0;
        
        /// <summary>
        /// カメラ録画・静止画の保存先ディレクトリ
        /// </summary>
        public string CameraSaveDirectory { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Videos");
        
        // カメラ設定
        /// <summary>
        /// 録画の解像度 - 幅（カメラから自動的に取得される実際の値）
        /// </summary>
        public int VideoWidth { get; set; } = 1280;
        
        /// <summary>
        /// 録画の解像度 - 高さ（カメラから自動的に取得される実際の値）
        /// </summary>
        public int VideoHeight { get; set; } = 720;
        
        /// <summary>
        /// 録画のフレームレート（カメラから自動的に取得される実際の値）
        /// </summary>
        public int FrameRate { get; set; } = 30;
        
        /// <summary>
        /// 映像コーデック (H264, MJPG, etc.)
        /// </summary>
        public string VideoCodec { get; set; } = "H264";
        
        /// <summary>
        /// 静止画の画質 (1-100)
        /// </summary>
        public int ImageQuality { get; set; } = 95;
        
        /// <summary>
        /// 静止画のフォーマット (png, jpg, etc.)
        /// </summary>
        public string ImageFormat { get; set; } = "png";

        /// <summary>
        /// コマンド受信用のUDPアドレス
        /// </summary>
        public string UdpListenAddress { get; set; } = "127.0.0.1:10001";

        /// <summary>
        /// UDP機能を有効にするかどうか
        /// </summary>
        public bool UdpEnabled { get; set; } = true;

        /// <summary>
        /// 録画停止トリガー未検出時のタイムアウト(分)
        /// </summary>
        public int RecordingTimeoutMinutes { get; set; } = 10;

        // コンストラクタ
        private AppSettings() { }

        /// <summary>
        /// 設定を読み込む
        /// </summary>
        /// <returns>読み込んだ設定、またはデフォルト設定</returns>
        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        Console.WriteLine("Settings loaded from file");
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
                global::LogWriter.AddErrorLog(ex, nameof(Load));
            }

            Console.WriteLine("Using default settings");
            return new AppSettings();
        }

        /// <summary>
        /// 設定を保存する
        /// </summary>
        public void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
                Console.WriteLine("Settings saved to file");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
                global::LogWriter.AddErrorLog(ex, nameof(Save));
            }
        }
    }
}
