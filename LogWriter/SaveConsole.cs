using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace micNotifyUDP.LogWriter
{
    // ===============================
    // Console.WriteLineの出力をファイルにリダイレクト
    // ===============================
    internal class SaveConsole : IDisposable // IDisposableを実装
    {
        private static readonly SaveConsole instance = new SaveConsole();

        public static SaveConsole Instance => instance;

        private FileStream fs;
        private StreamWriter sw;
        private MultiTextWriter multiTextWriter;

        private SaveConsole()
        {
            // ファイル名に現在日時を使う
            string dateTimeStr = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string dirName = "ConsoleLogs";
            string fileName = $"{dirName}/{dateTimeStr}.txt";

            // ディレクトリが存在しない場合、作成する
            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }

            // コンソール出力をファイルにリダイレクト
            fs = new FileStream(fileName, FileMode.Create);
            sw = new StreamWriter(fs);

            // StreamWriterはバッファリングを行うため、即座にファイルに出力されないことがあります。
            // StreamWriter.AutoFlushプロパティをtrueに設定することで、即時にファイルに書き込むように設定できます。
            // 一方で、頻繁に書き込みが行われる環境では、ディスクI/Oが頻繁に発生し、パフォーマンスに影響を与える可能性があります。。
            sw.AutoFlush = true;

            // A. これ以降のConsole.WriteLine()がファイルに書き込まれる
            // Console.SetOut(sw);

            // B. MultiTextWriterによってConsole.WriteLine()を複数の出力先にリダイレクト
            // DebugTextWriter クラスを用いて Console.WriteLine() の出力を Debug 出力にリダイレクト
            DebugTextWriter debugTextWriter = new DebugTextWriter();
            multiTextWriter = new MultiTextWriter(sw, debugTextWriter);

            // B. これ以降のConsole.WriteLine()がファイルとデバッグ出力に書き込まれる
            Console.SetOut(multiTextWriter);
        }

        // ログに日時を付加して出力
        public void WriteLog(string message)
        {
            string logMessage = $"{DateTime.Now:yyyy/MM/dd HH:mm:ss} {message}";
            Console.WriteLine(logMessage); // これがファイルに出力される

            // A. 即座にファイルに出力させるため、明示的にバッファをフラッシュするか、
            //sw.Flush();

            // B. MultiTextWriter を使用している場合は、MultiTextWriter.Flush(); ですべての TextWriter がFlushされる
            multiTextWriter.Flush();
        }

        // IDisposableを実装
        public void Dispose()
        {
            try
            {
                multiTextWriter?.Dispose(); // 新しく追加
            }
            finally
            {
                try
                {
                    // ストリームを閉じる
                    sw?.Dispose();
                }
                finally
                {
                    fs?.Dispose();
                }
            }
        }
    }
}
