using OpenCvSharp;
using System;
using System.Collections.Generic;
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
		private int fps;
		private int width;
		private int height;
		private string videoCodec;
		private int imageQuality;
		private string imageFormat;
		private int captureMode = 1; // デフォルトは動画モード (0=静止画, 1=動画)

		public event Action<string>? SnapshotSaved;
		public event Action<string>? VideoSaved;

		/// <summary>
		/// 現在の撮影モード（0=静止画, 1=動画）
		/// </summary>
		public int CaptureMode
		{
			get { return captureMode; }
			set { captureMode = (value == 0) ? 0 : 1; }
		}
		public CameraRecorder(int cameraIndex = 0, string saveDirectory = "Videos", int fps = 30, int captureMode = 1)
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
			this.captureMode = captureMode;

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

				// 設定後に実際の値を取得
				width = (int)capture.Get(VideoCaptureProperties.FrameWidth);
				height = (int)capture.Get(VideoCaptureProperties.FrameHeight);
				fps = (int)capture.Get(VideoCaptureProperties.Fps);

				// AppSettingsも更新して次回起動時に正確な値を使用できるようにする
				UpdateAppSettings();

				Console.WriteLine($"カメラの実際の設定値: 解像度={width}x{height}, FPS={fps}");
			}
		}

		/// <summary>
		/// AppSettingsをカメラの実際の値で更新する
		/// </summary>
		private void UpdateAppSettings()
		{
			var settings = AppSettings.Instance;
			bool changed = false;

			if (settings.VideoWidth != width)
			{
				settings.VideoWidth = width;
				changed = true;
			}

			if (settings.VideoHeight != height)
			{
				settings.VideoHeight = height;
				changed = true;
			}

			if (settings.FrameRate != fps)
			{
				settings.FrameRate = fps;
				changed = true;
			}

			// 変更があった場合のみ保存する
			if (changed)
			{
				settings.Save();
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
		}        /// <summary>
						 /// カメラの解像度を設定 (現在は無効化されており、実際の値が自動取得されます)
						 /// </summary>
						 /// <param name="width">幅</param>
						 /// <param name="height">高さ</param>
		public void SetResolution(int width, int height)
		{
			Console.WriteLine($"警告: 解像度設定は無効化されています。カメラから実際の値を自動取得します。");

			// すでにカメラがオープンされている場合は実際の値を取得
			if (capture != null && capture.IsOpened())
			{
				// 実際の値を取得
				this.width = (int)capture.Get(VideoCaptureProperties.FrameWidth);
				this.height = (int)capture.Get(VideoCaptureProperties.FrameHeight);

				// AppSettingsも更新
				UpdateAppSettings();

				Console.WriteLine($"カメラの実際の解像度: {this.width}x{this.height}");
			}
		}

		/// <summary>
		/// フレームレートを設定 (現在は無効化されており、実際の値が自動取得されます)
		/// </summary>
		/// <param name="frameRate">フレームレート</param>
		public void SetFrameRate(int frameRate)
		{
			Console.WriteLine($"警告: フレームレート設定は無効化されています。カメラから実際の値を自動取得します。");

			// すでにカメラがオープンされている場合は実際の値を取得
			if (capture != null && capture.IsOpened())
			{
				// 実際の値を取得
				this.fps = (int)capture.Get(VideoCaptureProperties.Fps);

				// AppSettingsも更新
				UpdateAppSettings();

				Console.WriteLine($"カメラの実際のフレームレート: {this.fps}");
			}
		}

		/// <summary>
		/// ビデオコーデックを設定
		/// </summary>
		/// <param name="codec">コーデック（H264, MJPG, DIVX, X264など）</param>
		public void SetVideoCodec(string codec)
		{
			if (!string.IsNullOrEmpty(codec))
			{
				this.videoCodec = codec;
			}
		}

		/// <summary>
		/// 画像品質を設定
		/// </summary>
		/// <param name="quality">品質（1-100）</param>
		public void SetImageQuality(int quality)
		{
			if (quality >= 1 && quality <= 100)
			{
				this.imageQuality = quality;
			}
		}

		/// <summary>
		/// 画像フォーマットを設定
		/// </summary>
		/// <param name="format">フォーマット（png, jpg, jpegなど）</param>
		public void SetImageFormat(string format)
		{
			if (!string.IsNullOrEmpty(format))
			{
				this.imageFormat = format.ToLower();
			}
		}

		/// <summary>
		/// 現在の設定をDictionaryとして取得
		/// </summary>
		/// <returns>設定を含むDictionary</returns>
		public Dictionary<string, object> GetSettings()
		{
			// カメラがまだ初期化されていない場合は初期化
			if (capture == null || !capture.IsOpened())
			{
				try
				{
					EnsureCapture();
				}
				catch
				{
					// カメラが利用できない場合は保存されている値を返す
				}
			}

			// カメラが利用可能な場合は実際の値を取得
			if (capture != null && capture.IsOpened())
			{
				width = (int)capture.Get(VideoCaptureProperties.FrameWidth);
				height = (int)capture.Get(VideoCaptureProperties.FrameHeight);
				fps = (int)capture.Get(VideoCaptureProperties.Fps);

				// AppSettingsも更新
				UpdateAppSettings();
			}
			return new Dictionary<string, object>
						{
								{ "resolution", $"{width}x{height}" },
								{ "frameRate", fps },
								{ "videoCodec", videoCodec },
								{ "imageQuality", imageQuality },
								{ "imageFormat", imageFormat },
								{ "mode", captureMode },
								{ "modeName", captureMode == 0 ? "静止画" : "動画" }
						};
		}

		/// <summary>
		/// カメラをセットアップし、実際の解像度とフレームレートを表示
		/// </summary>
		public void SetupCamera()
		{
			try
			{
				// カメラを初期化して実際の値を取得
				EnsureCapture();

				// 実際の値をコンソールに表示
				Console.WriteLine($"カメラの実際の設定: 解像度={width}x{height}, FPS={fps}");

				// イベントリスナーをアタッチしていない場合は追加
				if (SnapshotSaved == null)
				{
					SnapshotSaved += (path) => Console.WriteLine($"静止画を保存しました: {path}");
				}

				if (VideoSaved == null)
				{
					VideoSaved += (path) => Console.WriteLine($"録画を保存しました: {path}");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"カメラのセットアップに失敗しました: {ex.Message}");
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
