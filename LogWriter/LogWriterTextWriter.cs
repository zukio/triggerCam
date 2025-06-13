using System.IO;
using System.Text;

namespace triggerCam.LogWriter
{
    internal class LogWriterTextWriter : TextWriter
    {
        public override Encoding Encoding => Encoding.UTF8;

        public override void WriteLine(string? value)
        {
            global::LogWriter.AddLog(value ?? string.Empty);
        }

        public override void Write(string? value)
        {
            global::LogWriter.AddLog(value ?? string.Empty);
        }
    }
}
