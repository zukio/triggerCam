using System;
using System.IO;
using System.Linq;

namespace triggerCam.LogWriter
{
    /// <summary>
    /// ログファイルのローテーションを管理するクラス
    /// </summary>
    public class LogRotator
    {
        private readonly string logDirectory;
        private readonly int maxLogFiles;
        private readonly long maxLogSize;

        public LogRotator(string logDirectory, int maxLogFiles = 365, long maxLogSize = 365 * 1024 * 1024) // デフォルト: 7日分、10MB
        {
            this.logDirectory = logDirectory;
            this.maxLogFiles = maxLogFiles;
            this.maxLogSize = maxLogSize;
        }

        /// <summary>
        /// ログファイルのローテーションを実行
        /// </summary>
        public void RotateLogs()
        {
            try
            {
                if (!Directory.Exists(logDirectory))
                {
                    return;
                }

                // 古いログファイルを削除
                var logFiles = Directory.GetFiles(logDirectory, "*.log")
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .Skip(maxLogFiles);

                foreach (var file in logFiles)
                {
                    try
                    {
                        File.Delete(file);
                        Console.WriteLine($"Deleted old log file: {file}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting log file {file}: {ex.Message}");
                    }
                }

                // 現在のログファイルのサイズをチェック
                var currentLogFile = Path.Combine(logDirectory, $"{DateTime.Now:yyyyMMdd}.log");
                if (File.Exists(currentLogFile))
                {
                    var fileInfo = new FileInfo(currentLogFile);
                    if (fileInfo.Length > maxLogSize)
                    {
                        // バックアップファイル名を生成
                        var backupFile = Path.Combine(logDirectory, 
                            $"{DateTime.Now:yyyyMMdd}_{DateTime.Now:HHmmss}.log");
                        
                        // ファイルを移動
                        File.Move(currentLogFile, backupFile);
                        Console.WriteLine($"Rotated log file: {currentLogFile} -> {backupFile}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rotating logs: {ex.Message}");
            }
        }

        /// <summary>
        /// ログディレクトリの合計サイズを取得
        /// </summary>
        public long GetTotalLogSize()
        {
            try
            {
                if (!Directory.Exists(logDirectory))
                {
                    return 0;
                }

                return Directory.GetFiles(logDirectory, "*.log")
                    .Sum(f => new FileInfo(f).Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting total log size: {ex.Message}");
                return 0;
            }
        }
    }
}
