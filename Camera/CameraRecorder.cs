using OpenCvSharp;
using System;
using System.IO;
using System.Threading;
using triggerCam.Settings;

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
        private readonly int width;
        private readonly int height;
        private readonly string videoCodec;
        private readonly int imageQuality;
        private readonly string imageFormat;

        public event Action<string>? SnapshotSaved;
        public event Action<string>? VideoSaved;

        public CameraRecorder(int cameraIndex = 0, string saveDirectory = "Videos", int fps = 30)
        {
            var settings = AppSettings.Instance;
            this.cameraIndex = cameraIndex;
            this.saveDirectory = saveDirectory;
            this.fps = settings.FrameRate > 0 ? settings.FrameRate : fps;
            this.width = settings.VideoWidth > 0 ? settings.VideoWidth : 1280;
            this.height = settings.VideoHeight > 0 ? settings.VideoHeight : 720;
            this.videoCodec = !string.IsNullOrEmpty(settings.VideoCodec) ? settings.VideoCodec : "H264";
            this.imageQuality = settings.ImageQuality > 0 ? settings.ImageQuality : 95;
            this.imageFormat = !string.IsNullOrEmpty(settings.ImageFormat) ? settings.ImageFormat.ToLower() : "png";
            
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
                // リサイズが必要な場合
                if (frame.Width != width || frame.Height != height)
                {
                    Cv2.Resize(frame, frame, new OpenCvSharp.Size(width, height));
                }
                
                string extension = imageFormat.ToLower();
                if (extension != "jpg" && extension != "jpeg" && extension != "png")
                {
                    extension = "png"; // デフォルト
                }
                
                string path = Path.Combine(saveDirectory, fileName + "." + extension);
                
                // 画質設定
                var imwriteParams = new int[] { (int)ImwriteFlags.JpegQuality, imageQuality };
                
                Cv2.ImWrite(path, frame, imwriteParams);
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
            
            // FourCCコードを取得
            var fourCC = GetFourCC(videoCodec);
            
            writer = new VideoWriter(path, fourCC, fps, new OpenCvSharp.Size(width, height));
            
            if (!writer.IsOpened())
            {
                // H264がサポートされていない場合はMJPGにフォールバック
                writer.Dispose();
                writer = new VideoWriter(path, FourCC.MJPG, fps, new OpenCvSharp.Size(width, height));
            }
            
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
            using var resizedFrame = new Mat();
            
            while (isRecording && capture!.Read(frame))
            {
                if (frame.Empty()) continue;
                
                // リサイズが必要な場合
                if (frame.Width != width || frame.Height != height)
                {
                    Cv2.Resize(frame, resizedFrame, new OpenCvSharp.Size(width, height));
                    writer!.Write(resizedFrame);
                }
                else
                {
                    writer!.Write(frame);
                }
                
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
                
                // カメラの解像度を設定
                capture.Set(VideoCaptureProperties.FrameWidth, width);
                capture.Set(VideoCaptureProperties.FrameHeight, height);
            }
        }
        
        /// <summary>
        /// 文字列からFourCCコードを取得
        /// </summary>
        private FourCC GetFourCC(string codec)
        {
            switch (codec.ToUpper())
            {
                case "H264": return FourCC.H264;
                case "MJPG": return FourCC.MJPG;
                case "DIVX": return FourCC.DIVX;
                case "X264": return FourCC.X264;
                default: return FourCC.H264;
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
