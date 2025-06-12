/**
 * 指定したパスにログファイルを出力するクラスライブラリ
 * 通常のログとエラーログを分けることができる
 * また Unity で使用する場合は Debug.Log() も実行する
 */

// どのアプリケーションで使用するかを設定
#define WPF
//#deifne UNITY_EDITOR
//#define UNITY_EXE

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

public static class LogWriter
{
    // ログファイルを出力するパスの設定
#if WPF
    public static string _logDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Logs";
#endif
#if UNITY_EDITOR
    // Unity Editor のときはプロジェクトのトップにフォルダを作成
    public static string _logDir = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
#endif
#if UNITY_EXE
    // Unity の exe ファイルのときは .exe ファイルと同じフォルダに書き出す
    public static string _logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\'), "Logs");
#endif

    // キューを保存する変数
    private static ConcurrentQueue<string> _q_Log;
    public static ConcurrentQueue<string> q_Log
    {
        get
        {
            if (_q_Log == null)
                _q_Log = new ConcurrentQueue<string>();
            return _q_Log;
        }
    }

    // ログを追記する際に使用するスレッド制限
    private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);





    /// <summary>
    /// ログを出力する
    /// </summary>
    /// <param name="text">ログ</param>
    /// <param name="addLogFile">ログファイルに追記を行うか</param>
    public static void AddLog(string text, bool addLogFile = true)
    {
        string dateTime = DateTime.Now.ToString("[MM/dd HH:mm:ss.fff]");
        q_Log.Enqueue(dateTime + text);

        // ログファイルに追記する場合は実行
        if (addLogFile)
            AddInfo(text, dateTime);
    }

    /// <summary>
    /// エラーログを出力する
    /// </summary>
    /// <param name="error">例外</param>
    /// <param name="methodName">発生したメソッド名</param>
    public static void AddErrorLog(Exception error, string methodName = "")
    {
        string dateTime = DateTime.Now.ToString("[MM/dd HH:mm:ss.fff]");
        q_Log.Enqueue($"{dateTime} *-* ERROR *-* {(methodName == "" ? "" : $"@{methodName}")} : {error}");
        AddError(error, methodName, dateTime);
    }

    /// <summary>
    /// エラーログを出力
    /// </summary>
    /// <param name="text">ログ</param>
    /// <param name="methodName">発生したメソッド名</param>
    public static void AddErrorLog(string text, string methodName = "")
    {
        string dateTime = DateTime.Now.ToString("[MM/dd HH:mm:ss.fff]");
        q_Log.Enqueue($"{dateTime} *-* ERROR *-* {(methodName == "" ? "" : $"@{methodName}")} : {text}");
        AddError(text, methodName, dateTime);
    }


    private static async void AddInfo(string text, string date)
    {
        string _date = date;
        string dir = _logDir;
        string fileName = $"Log_{DateTime.Now.ToString("yyyy_MM_dd")}.txt";
        string fullpath = Path.Combine(dir, fileName);

        // ディレクトリが存在しない場合は作成
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        // ロックを取得
        await _semaphore.WaitAsync();
        using (StreamWriter streamWriter = new StreamWriter(fullpath, true, Encoding.UTF8))
        {
            try
            {
                var parentDir = new FileInfo(fullpath).Directory;
                // ディレクトリが存在しない場合は新規作成
                if (parentDir != null && !parentDir.Exists)
                    parentDir.Create();

                // ログを追記
                await streamWriter.WriteLineAsync($"{_date} {text}");
            }
            finally
            {
                // ロックの開放
                _semaphore.Release();
            }

        }
    }

    private static async void AddError(Exception error, string methodName, string date)
    {
        string _date = date;
        string dir = _logDir + "_Error";
        string fileName = $"ErrorLog_{DateTime.Now.ToString("yyyy_MM_dd")}.txt";
        string fullpath = Path.Combine(dir, fileName);

        // ディレクトリが存在しない場合は作成
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        // ロックの取得
        await _semaphore.WaitAsync();
        using (StreamWriter streamWriter = new StreamWriter(fullpath, true, Encoding.UTF8))
        {
            try
            {
                var parentDir = new FileInfo(fullpath).Directory;
                // ディレクトリが存在しない場合は新規作成
                if (parentDir != null && !parentDir.Exists)
                    parentDir.Create();

                // ログを追記
                await streamWriter.WriteLineAsync($"{_date} {(methodName == "" ? "" : $"@{methodName} ")}: {error}");
            }
            finally
            {
                // ロックの開放
                _semaphore.Release();
            }
        }
    }

    private static async void AddError(string text, string methodName, string date)
    {
        string _date = date;
        string dir = _logDir + "_Error";
        string fileName = $"ErrorLog_{DateTime.Now.ToString("yyyy_MM_dd")}.txt";
        string fullpath = Path.Combine(dir, fileName);

        // ディレクトリが存在しない場合は作成
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        // ロックの取得
        await _semaphore.WaitAsync();
        using (StreamWriter streamWriter = new StreamWriter(fullpath, true, Encoding.UTF8))
        {
            try
            {
                var parentDir = new FileInfo(fullpath).Directory;
                // ディレクトリが存在しない場合は新規作成
                if (parentDir != null && !parentDir.Exists)
                    parentDir.Create();

                // ログを追記
                await streamWriter.WriteLineAsync($"{_date} {(methodName == "" ? "" : $"@{methodName} ")}: {text}");
            }
            finally
            {
                // ロックの開放
                _semaphore.Release();
            }
        }
    }
}

/* ログを表示する場合は MainWindow.cs 等に以下を追記する
 * 変数
    List<string> logList = new List<string>();
    StringBuilder sbLog = new StringBuilder();
 * Window_Loaded 内
    CompositionTarget.Rendering += CompositionTargetRendering;

 * CompositionTargetRendering メソッド
    string? result = "";
    if (LogWriter.q_Log.Count > 0)
    {
        if (LogWriter.q_Log.TryDequeue(out result))
        {
            logList.Add(result);
            while (logList.Count > 20)
            logList.RemoveAt(0);

            foreach (var log in logList)
            sbLog.AppendLine(log.Replace("\r\n", ""));
            textJson.Text = sbLog.ToString();
            sbLog.Clear();
        }
    }
*/