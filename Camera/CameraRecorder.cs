using OpenCvSharp;
using System;
using System.IO;
using System.Threading;

namespace triggerCam.Camera
{
    /// <summary>
    /// Webカメラから静止画・動画を保存するクラス
    /// </summary>
    public class CameraRecorder : IDisposable
    {
        private VideoCapture? capture;
        private VideoWriter? writer;
        private Thread? recordThread;
        private bool isRecording = false;
        private readonly int cameraIndex;
        private readonly string saveDirectory;
        private readonly int fps;

        public event Action<string>? SnapshotSaved;
        public event Action<string>? VideoSaved;

        public CameraRecorder(int cameraIndex = 0, string saveDirectory = "Videos", int fps = 30)
        {
            this.cameraIndex = cameraIndex;
            this.saveDirectory = saveDirectory;
            this.fps = fps;
            Directory.CreateDirectory(saveDirectory);
        }

        /// <summary>
        /// 静止画を撮影して保存
        /// </summary>
        public void TakeSnapshot(string fileName)
        {
            EnsureCapture();
            using var frame = new Mat();
            if (capture!.Read(frame) && !frame.Empty())
            {
                string path = Path.Combine(saveDirectory, fileName + ".png");
                Cv2.ImWrite(path, frame);
                SnapshotSaved?.Invoke(path);
            }
        }

        /// <summary>
        /// 動画の録画を開始
        /// </summary>
        public void StartRecording(string fileName)
        {
            if (isRecording) return;
            EnsureCapture();
            string path = Path.Combine(saveDirectory, fileName + ".mp4");
            writer = new VideoWriter(path, FourCC.H264, fps, new OpenCvSharp.Size(capture!.FrameWidth, capture.FrameHeight));
            isRecording = true;
            recordThread = new Thread(() => RecordLoop(path));
            recordThread.Start();
        }

        /// <summary>
        /// 動画の録画を停止
        /// </summary>
        public void StopRecording()
        {
            if (!isRecording) return;
            isRecording = false;
            recordThread?.Join();
            writer?.Dispose();
            writer = null;
        }

        private void RecordLoop(string path)
        {
            using var frame = new Mat();
            while (isRecording && capture!.Read(frame))
            {
                if (frame.Empty()) continue;
                writer!.Write(frame);
                Cv2.WaitKey(1);
            }
            VideoSaved?.Invoke(path);
        }

        private void EnsureCapture()
        {
            if (capture == null)
            {
                capture = new VideoCapture(cameraIndex);
                if (!capture.IsOpened())
                {
                    throw new InvalidOperationException("Camera open failed");
                }
            }
        }

        public void Dispose()
        {
            StopRecording();
            capture?.Release();
            capture?.Dispose();
            capture = null;
        }
    }
}
