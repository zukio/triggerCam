using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace triggerCam.LogWriter
{
    // ===============================
    // Console.WriteLineの出力をDebug 出力にリダイレクト
    // ===============================
    public class DebugTextWriter : TextWriter
    {
        public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;

        public override void WriteLine(string? value)
        {
            Debug.WriteLine(value);
        }

        public override void Write(string? value)
        {
            Debug.Write(value);
        }
    }

    // ===============================
    // Console.WriteLineの出力を複数の TextWriter にリダイレクトする
    // ===============================
    public class MultiTextWriter : TextWriter, IDisposable
    {
        private readonly TextWriter[] _writers;

        public override Encoding Encoding => Encoding.UTF8;

        // 例：Console.WriteLine() を、ファイル出力（fs）とデバッグ出力（DebugTextWriter）にリダイレクトする
        public MultiTextWriter(params TextWriter[] writers)
        {
            _writers = writers;
        }

        public new void Dispose()
        {
            foreach (var writer in _writers)
            {
                writer?.Dispose();
            }
        }

        public override void WriteLine(string? value)
        {
            foreach (var writer in _writers)
            {
                writer.WriteLine(value);
            }
        }

        public override void Write(string? value)
        {
            foreach (var writer in _writers)
            {
                writer.Write(value);
            }
        }

        public override void Flush()
        {
            foreach (var writer in _writers)
            {
                writer.Flush();
            }
        }
    }

}
