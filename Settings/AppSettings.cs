using System.Text.Json;

namespace micNotifyUDP.Settings
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

        // 設定項目
        public string UdpToAddress { get; set; } = "127.0.0.1:10000";
        public string UdpListenAddress { get; set; } = "127.0.0.1:10001";
        public int SilenceThreshold { get; set; } = 150;
        public int MinAudioSamples { get; set; } = 10;
        public string DeviceName { get; set; } = "";
        public string RecordingsDirectory { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recordings");
				public bool UseTempFile { get; set; } = true;
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
            }
        }
    }
}
