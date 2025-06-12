using NAudio.CoreAudioApi;
using NAudio.Wave;
using triggerCam.Settings;

namespace triggerCam.Audio
{
    /// <summary>
    /// マイク入力を録音するクラス
    /// </summary>
    public class AudioRecorder
    {
        private WaveInEvent? waveIn;
        private WaveFileWriter? writer;
        private string currentFilePath = "";
        private string? customRecordingPath = null;
        private bool useTempFile = true;
        
        // 録音データのサイズを追跡
        private long recordedBytesCount = 0;
        private bool hasAudioData = false;
        
        // 音声検出のための変数
        private int silenceThreshold; // 無音とみなす閾値（0-32767）
        private int minAudioSamples; // 音声ありと判定するために必要な閾値を超えるサンプル数
        private int audioSamplesCount = 0; // 閾値を超えたサンプル数
        private float maxLevel = 0; // 最大音量レベル
        
        // 設定
        private AppSettings settings;

        public AudioRecorder()
        {
            // 設定を読み込む
            settings = AppSettings.Instance;
            
            // 録音ファイルを保存するディレクトリを作成
            if (!Directory.Exists(settings.RecordingsDirectory))
            {
                Directory.CreateDirectory(settings.RecordingsDirectory);
            }
            
            // 設定から閾値と一時ファイル設定を読み込む
            LoadSettings();
        }
        
        /// <summary>
        /// 設定を読み込む
        /// </summary>
        public void LoadSettings()
        {
            silenceThreshold = settings.SilenceThreshold;
            minAudioSamples = settings.MinAudioSamples;
            useTempFile = settings.UseTempFile;
            Console.WriteLine($"Loaded settings: Silence={silenceThreshold}, MinSamples={minAudioSamples}, UseTempFile={useTempFile}");
        }
        
        /// <summary>
        /// 閾値設定を更新する
        /// </summary>
        /// <param name="threshold">新しい閾値</param>
        /// <param name="minSamples">新しい最小サンプル数</param>
        public void UpdateThresholdSettings(int threshold, int minSamples)
        {
            silenceThreshold = threshold;
            minAudioSamples = minSamples;
            
            // 設定を保存
            settings.SilenceThreshold = threshold;
            settings.MinAudioSamples = minSamples;
            settings.Save();
            
            Console.WriteLine($"Updated threshold settings: Silence={silenceThreshold}, MinSamples={minAudioSamples}");
        }

        /// <summary>
        /// 一時ファイル設定を更新する
        /// </summary>
        /// <param name="useTempFile">一時ファイルを使用するかどうか</param>
        public void UpdateTempFileSettings(bool useTempFile)
        {
            this.useTempFile = useTempFile;
            
            // 設定を保存
            settings.UseTempFile = useTempFile;
            settings.Save();
            
            Console.WriteLine($"Updated temp file settings: UseTempFile={useTempFile}");
        }

        /// <summary>
        /// 録音を開始する
        /// </summary>
        /// <param name="device">録音に使用するデバイス</param>
        /// <param name="fileName">録音ファイル名（拡張子なし）</param>
        /// <param name="customPath">カスタム保存パス（オプション）</param>
        public void StartRecording(MMDevice device, string fileName, string? customPath = null)
        {
            // 既に録音中の場合は停止
            if (waveIn != null)
            {
                StopRecording();
            }

            try
            {
                // 録音データのリセット
                recordedBytesCount = 0;
                hasAudioData = false;
                audioSamplesCount = 0;
                maxLevel = 0;
                
                // カスタムパスを保存（一時的に使用）
                customRecordingPath = customPath;
                
                // 録音ファイルのパスを設定
                string directory;
                
                if (useTempFile)
                {
                    // 一時ファイルを使用する場合
                    directory = Path.GetTempPath();
                }
                else
                {
                    // 永続的なファイルを使用する場合（優先順位：カスタムパス > 設定値）
                    directory = customPath ?? settings.RecordingsDirectory;
                }
                
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                currentFilePath = Path.Combine(directory, $"{fileName}.wav");

                // WaveInEventを初期化
                waveIn = new WaveInEvent
                {
                    DeviceNumber = GetDeviceNumber(device.FriendlyName),
                    WaveFormat = new WaveFormat(44100, 16, 1) // 44.1kHz, 16bit, モノラル
                };

                // 録音ファイルを作成
                writer = new WaveFileWriter(currentFilePath, waveIn.WaveFormat);

                // データ受信イベントを設定
                waveIn.DataAvailable += OnDataAvailable;
                waveIn.RecordingStopped += OnRecordingStopped;

                // 録音開始
                waveIn.StartRecording();
                Console.WriteLine($"Recording started: {currentFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting recording: {ex.Message}");
                CleanupRecording();
                throw;
            }
        }

        /// <summary>
        /// 録音を停止する
        /// </summary>
        /// <param name="saveToFile">一時ファイルを永続的なファイルに保存するかどうか（一時ファイル使用時のみ有効）</param>
        /// <returns>録音ファイルのパス。音声が空の場合は空文字列</returns>
        public string StopRecording(bool saveToFile = false)
        {
            string filePath = currentFilePath;
            
            try
            {
                if (waveIn != null)
                {
                    waveIn.StopRecording();
                    // OnRecordingStoppedイベントが呼ばれるのを待つ
                    System.Threading.Thread.Sleep(500);
                }
            }
            finally
            {
                // 確実にリソースを解放
                CleanupRecording();
            }
            
            // 録音データが空かチェック
            if (!hasAudioData || audioSamplesCount < minAudioSamples || maxLevel < (silenceThreshold * 2))
            {
                Console.WriteLine("Recording is empty, deleting file");
                
                // ファイルが存在する場合は削除（リトライロジック付き）
                if (File.Exists(filePath))
                {
                    DeleteFileWithRetry(filePath, 3);
                }
                
                return ""; // 空の場合は空文字列を返す
            }
            
            // 一時ファイルを使用していて、永続的に保存する場合
            if (useTempFile && saveToFile && !string.IsNullOrEmpty(filePath))
            {
                try
                {
                    // 永続的な保存先ディレクトリを設定
                    string permanentDirectory = customRecordingPath ?? settings.RecordingsDirectory;
                    if (!Directory.Exists(permanentDirectory))
                    {
                        Directory.CreateDirectory(permanentDirectory);
                    }
                    
                    // ファイル名を取得
                    string fileName = Path.GetFileName(filePath);
                    string permanentFilePath = Path.Combine(permanentDirectory, fileName);
                    
                    // ファイルをコピー
                    File.Copy(filePath, permanentFilePath, true);
                    Console.WriteLine($"Copied temporary file to permanent location: {permanentFilePath}");
                    
                    // 一時ファイルを削除
                    DeleteFileWithRetry(filePath, 3);
                    
                    // 永続的なファイルパスを返す
                    return permanentFilePath;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error copying temporary file to permanent location: {ex.Message}");
                    // エラーが発生した場合は一時ファイルのパスを返す
                    return filePath;
                }
            }

            return filePath;
        }
        
        /// <summary>
        /// ファイルを削除する（リトライロジック付き）
        /// </summary>
        /// <param name="filePath">削除するファイルのパス</param>
        /// <param name="retryCount">リトライ回数</param>
        private void DeleteFileWithRetry(string filePath, int retryCount)
        {
            int attempts = 0;
            bool deleted = false;
            
            while (!deleted && attempts < retryCount)
            {
                try
                {
                    // GCを強制実行してファイルハンドルを解放
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    
                    // ファイルを削除
                    File.Delete(filePath);
                    deleted = true;
                    Console.WriteLine($"Successfully deleted empty recording file: {filePath}");
                }
                catch (Exception ex)
                {
                    attempts++;
                    Console.WriteLine($"Error deleting empty recording file (attempt {attempts}): {ex.Message}");
                    
                    if (attempts < retryCount)
                    {
                        // 少し待ってからリトライ
                        System.Threading.Thread.Sleep(500);
                    }
                }
            }
        }

        /// <summary>
        /// データ受信イベントハンドラ
        /// </summary>
        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (writer != null && e.BytesRecorded > 0)
            {
                writer.Write(e.Buffer, 0, e.BytesRecorded);
                recordedBytesCount += e.BytesRecorded;
                
                // 音声データの分析
                int samplesOverThreshold = 0;
                float currentMaxLevel = 0;
                
                // バッファ内のサンプルを分析
                for (int i = 0; i < e.BytesRecorded; i += 2) // 16ビットサンプルなので2バイトずつ
                {
                    if (i + 1 < e.BytesRecorded)
                    {
                        short sample = (short)((e.Buffer[i + 1] << 8) | e.Buffer[i]);
                        float level = Math.Abs(sample);
                        
                        // 最大レベルを更新
                        if (level > currentMaxLevel)
                            currentMaxLevel = level;
                            
                        // 閾値を超えるサンプルをカウント
                        if (level > silenceThreshold)
                            samplesOverThreshold++;
                    }
                }
                
                // 最大レベルを更新
                if (currentMaxLevel > maxLevel)
                    maxLevel = currentMaxLevel;
                
                // 閾値を超えるサンプル数を加算
                audioSamplesCount += samplesOverThreshold;
                
                // デバッグ出力（定期的に）
                if (recordedBytesCount % 100000 < 2000) // 約100KBごとに出力
                {
                    Console.WriteLine($"Audio analysis - Max level: {maxLevel}, Samples over threshold: {audioSamplesCount}");
                }
                
                // 音声データがあるかチェック
                if (!hasAudioData && audioSamplesCount >= minAudioSamples)
                {
                    hasAudioData = true;
                    Console.WriteLine($"Audio detected! Samples over threshold: {audioSamplesCount}, Max level: {maxLevel}");
                }
            }
        }

        /// <summary>
        /// 録音停止イベントハンドラ
        /// </summary>
        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            CleanupRecording();
            Console.WriteLine($"Recording stopped. Recorded bytes: {recordedBytesCount}, Audio samples: {audioSamplesCount}, Max level: {maxLevel}, Has audio data: {hasAudioData}");
        }

        /// <summary>
        /// 録音リソースをクリーンアップする
        /// </summary>
        private void CleanupRecording()
        {
            try
            {
                if (waveIn != null)
                {
                    waveIn.DataAvailable -= OnDataAvailable;
                    waveIn.RecordingStopped -= OnRecordingStopped;
                    waveIn.Dispose();
                    waveIn = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning up waveIn: {ex.Message}");
            }

            try
            {
                if (writer != null)
                {
                    // ファイルを確実に閉じる
                    writer.Flush();
                    writer.Dispose();
                    writer = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning up writer: {ex.Message}");
            }
            
            // GCを促す
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        /// <summary>
        /// デバイス名からデバイス番号を取得する
        /// </summary>
        /// <param name="deviceName">デバイス名</param>
        /// <returns>デバイス番号</returns>
        private int GetDeviceNumber(string deviceName)
        {
            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                var capabilities = WaveIn.GetCapabilities(i);
                if (deviceName.Contains(capabilities.ProductName))
                {
                    return i;
                }
            }

            // デバイスが見つからない場合はデフォルトデバイス（0）を使用
            Console.WriteLine($"Device not found: {deviceName}, using default device");
            return 0;
        }

        /// <summary>
        /// リソースを解放する
        /// </summary>
        public void Dispose()
        {
            CleanupRecording();
        }
    }
}