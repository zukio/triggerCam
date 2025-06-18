using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO.Ports;
using System.Threading;
using DirectShowLib;
using triggerCam.Camera;
using triggerCam.Settings;
using triggerCam.UDP;

namespace triggerCam
{
	internal static class Program
	{
		private static SerialTriggerListener? serialListener;
		private static CameraRecorder? cameraRecorder;
		private static UdpClient? udpClient;
		private static UDPServer? udpServer;
		private static CommandProcessor? commandProcessor;
		private static TrayIcon? trayIcon;
		private static System.Windows.Forms.Timer? udpTimer;
		private static System.Windows.Forms.Timer? recordingTimeoutTimer;
		private static string snapshotSource = "success";
		private static string recordSource = "success";
		private static string udpToIP = "127.0.0.1";
		private static int udpToPort = 10000;
		private static string udpListenIP = "127.0.0.1";
		private static int udpListenPort = 10001;
		private static int recordingTimeoutMinutes = 10;
		private static Mutex? appMutex;

		[STAThread]
		static void Main(string[] args)
		{
			bool createdNew;
			appMutex = new Mutex(true, "triggerCamAppMutex", out createdNew);
			if (!createdNew)
			{
				MessageBox.Show(
						"すでに起動中です",
						"triggerCam",
						MessageBoxButtons.OK,
						MessageBoxIcon.Information);
				return;
			}

			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				MessageBox.Show(
						"Serial features are only available on Windows.",
						"Platform Not Supported",
						MessageBoxButtons.OK,
						MessageBoxIcon.Information);
				return;
			}

			// Initialize log redirection
			var consoleSaver = triggerCam.LogWriter.SaveConsole.Instance;

			var settings = AppSettings.Instance;
			recordingTimeoutMinutes = settings.RecordingTimeoutMinutes;

			// コマンドライン引数の処理
			ProcessCommandLineArgs(args, settings);

			// UDP送信先の設定
			ParseUdpAddress(settings.UdpToAddress, out udpToIP, out udpToPort);

			// UDP受信の設定
			ParseUdpAddress(settings.UdpListenAddress, out udpListenIP, out udpListenPort);

                        // カメラレコーダーの初期化
                        cameraRecorder = new CameraRecorder(
                                        settings.CameraIndex,
                                        settings.CameraSaveDirectory,
                                        settings.FrameRate,
                                        settings.CaptureMode);
			cameraRecorder.SnapshotSaved += path =>
			{
				var data = new Dictionary<string, object> { { "path", path } };
				Notify(snapshotSource, "SnapSaved", data);
			};
			cameraRecorder.VideoSaved += path =>
			{
				var data = new Dictionary<string, object> { { "path", path } };
				Notify(recordSource, "RecStop", data);
				trayIcon?.UpdateRecordingState(false);
			};

			// カメラをセットアップして実際の解像度とフレームレートを取得
			cameraRecorder.SetupCamera();
			var camName = GetCameraName(settings.CameraIndex);
			var conData = new Dictionary<string, object>
						{
								{ "id", settings.CameraIndex },
								{ "name", camName }
						};
			Notify("success", "Connected", conData);

                        // シリアルトリガーリスナーの初期化
                        InitializeSerialListener(settings);

			ApplicationConfiguration.Initialize();

			// タスクトレイアイコンの初期化
			trayIcon = new TrayIcon();

			if (settings.UdpEnabled)
			{
				StartUdpServices();
			}
			Application.ApplicationExit += (s, e) =>
			{
				// CleanupBeforeExitから呼び出されていない場合のみクリーンアップを実行
				if (udpServer != null || serialListener != null || cameraRecorder != null)
				{
					StopUdpServices();
					serialListener?.Dispose();
					cameraRecorder?.Dispose();
					Notify("success", "disConnected");
				}

				// 常に実行する必要のあるクリーンアップ処理
				triggerCam.LogWriter.SaveConsole.Instance.Dispose();
				appMutex?.ReleaseMutex();
				appMutex?.Dispose();
			};

			Application.Run();
		}

		private static void SendUdp(string message)
		{
			if (udpClient != null && AppSettings.Instance.UdpEnabled)
			{
				UDPSender.SendUDP(udpClient, message, udpToIP, udpToPort);
			}
		}

		public static void SetSnapshotSource(string src) => snapshotSource = src;
		public static void SetRecordSource(string src) => recordSource = src;

		public static void Notify(string status, string message, Dictionary<string, object>? data = null)
		{
			if (udpClient != null && AppSettings.Instance.UdpEnabled)
			{
				UdpNotifier.SendStatus(udpClient, udpToIP, udpToPort, status, message, data);
			}
		}

		private static string GetCameraName(int index)
		{
			try
			{
				var devices = new List<DsDevice>(DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice));
				if (index >= 0 && index < devices.Count)
				{
					return devices[index].Name;
				}
			}
			catch (Exception ex)
			{
				global::LogWriter.AddErrorLog(ex, nameof(GetCameraName));
			}
			return $"Camera {index}";
		}

		private static void ParseUdpAddress(string address, out string ip, out int port)
		{
			ip = "127.0.0.1";
			port = 10000;
			if (!string.IsNullOrEmpty(address))
			{
				var parts = address.Split(':');
				if (parts.Length == 2 && int.TryParse(parts[1], out int p))
				{
					ip = parts[0];
					port = p;
				}
			}
		}

		private static string CreateFileName() => DateTime.Now.ToString("yyyyMMdd_HHmmss");

		private static void StartUdpServices()
		{
			if (udpClient != null)
				return;

			udpClient = new UdpClient();
			udpServer = new UDPServer(udpListenIP, udpListenPort);
			commandProcessor = new CommandProcessor(
					udpClient,
					udpToIP,
					udpToPort,
					cameraRecorder!,
					trayIcon!);

			udpTimer = new System.Windows.Forms.Timer
			{
				Interval = 100
			};

			udpTimer.Tick += (sender, e) =>
			{
				while (UDPServer_Static.q_UdpData.TryDequeue(out UDP_DATA? data))
				{
					if (data != null)
					{
						commandProcessor?.ProcessCommand(data);
					}
				}
			};

			udpTimer.Start();
		}
		private static void StopUdpServices()
		{
			udpTimer?.Stop();
			udpTimer?.Dispose();
			udpTimer = null;

			udpClient?.Dispose();
			udpClient = null;

			udpServer?.Dispose();
			udpServer = null;

			commandProcessor = null;
		}

		/// <summary>
		/// アプリケーション終了前にリソースを安全にクリーンアップします
		/// </summary>
		public static void CleanupBeforeExit()
		{
			// UDPサーバーを停止
			StopUdpServices();

			// シリアルリスナーを停止
			serialListener?.Dispose();
			serialListener = null;

			// カメラレコーダーを停止
			cameraRecorder?.Dispose();
			cameraRecorder = null;

			// 少し待機して、実行中のコールバックが完了するのを待つ
			Thread.Sleep(200);

			// ログ記録を停止（このタイミングで安全にDisposeできる）
			Notify("success", "disConnected");
		}

        public static void UpdateUdpEnabled(bool enabled)
        {
                var settings = AppSettings.Instance;
                settings.UdpEnabled = enabled;

			if (enabled)
			{
				StartUdpServices();
			}
			else
			{
				StopUdpServices();
                }
        }

        public static void UpdateSerialSettings(AppSettings settings)
        {
                InitializeSerialListener(settings);
        }

        public static void UpdateCameraSettings(AppSettings settings)
        {
                Notify("success", "disConnected");
                cameraRecorder?.Dispose();

                cameraRecorder = new CameraRecorder(
                                settings.CameraIndex,
                                settings.CameraSaveDirectory,
                                settings.FrameRate,
                                settings.CaptureMode);

                cameraRecorder.SnapshotSaved += path =>
                {
                        var data = new Dictionary<string, object> { { "path", path } };
                        Notify(snapshotSource, "SnapSaved", data);
                };

                cameraRecorder.VideoSaved += path =>
                {
                        var data = new Dictionary<string, object> { { "path", path } };
                        Notify(recordSource, "RecStop", data);
                        trayIcon?.UpdateRecordingState(false);
                };

                cameraRecorder.SetupCamera();
                var camName = GetCameraName(settings.CameraIndex);
                var conData = new Dictionary<string, object>
                                {
                                                { "id", settings.CameraIndex },
                                                { "name", camName }
                                };
                Notify("success", "Connected", conData);
        }

        private static void InitializeSerialListener(AppSettings settings)
        {
                serialListener?.Dispose();

                string portName = settings.ComPort;
                var ports = SerialPort.GetPortNames();
                if (!string.IsNullOrEmpty(portName) && !ports.Contains(portName, StringComparer.OrdinalIgnoreCase))
                {
                        if (ports.Length > 0)
                        {
                                MessageBox.Show(
                                        $"指定されたポート {portName} が見つかりません。{ports[0]} を使用します。",
                                        "COMポート警告",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Warning);
                                portName = ports[0];
                                settings.ComPort = portName;
                                settings.Save();
                        }
                }

                serialListener = new SerialTriggerListener(
                                portName,

                                settings.BaudRate,
                                settings.SnapTrigger,
                                settings.StartTrigger,
                                settings.StopTrigger);
                serialListener.SnapReceived += () =>
                {
                        SetSnapshotSource("serial");
                        cameraRecorder!.TakeSnapshot(CreateFileName());
                };
                serialListener.StartReceived += () =>
                {
                        SetRecordSource("serial");
                        cameraRecorder!.StartRecording(CreateFileName());
                        Notify("serial", "RecStart");
                        trayIcon?.UpdateRecordingState(true);
                        StartRecordingTimeout();
                };
                serialListener.StopReceived += () =>
                {
                        SetRecordSource("serial");
                        cameraRecorder!.StopRecording();
                        StopRecordingTimeout();
                };

                serialListener.Start();
        }

		/// <summary>
		/// コマンドライン引数を処理する
		/// </summary>
		/// <param name="args">コマンドライン引数</param>
		/// <param name="settings">アプリケーション設定</param>
		private static void ProcessCommandLineArgs(string[] args, AppSettings settings)
		{
			if (args == null || args.Length == 0)
				return;

			for (int i = 0; i < args.Length; i++)
			{
				string arg = args[i].ToLower();
				if (i + 1 < args.Length)
				{
					string value = args[i + 1];

					switch (arg)
					{
						case "--com":
						case "-c":
							settings.ComPort = value;
							i++;
							break;

                                                case "--baud":
                                                case "-b":
                                                        if (int.TryParse(value, out int baudRate))
                                                        {
                                                                settings.BaudRate = baudRate;
                                                        }
                                                        i++;
                                                        break;

                                                case "--snap":
                                                        settings.SnapTrigger = value;
                                                        i++;
                                                        break;

                                                case "--starttrig":
                                                        settings.StartTrigger = value;
                                                        i++;
                                                        break;

                                                case "--stoptrig":
                                                        settings.StopTrigger = value;
                                                        i++;
                                                        break;

						case "--camera":
						case "-cam":
							if (int.TryParse(value, out int cameraIndex))
							{
								settings.CameraIndex = cameraIndex;
							}
							i++;
							break;
						case "--resolution":
						case "-r":
							// 解像度はカメラから自動的に取得されるため、このコマンドは無視されます
							Console.WriteLine("注意: 解像度はカメラから自動的に取得されるようになりました。この設定は無視されます。");
							i++;
							break;

						case "--fps":
						case "-f":
							// フレームレートはカメラから自動的に取得されるため、このコマンドは無視されます
							Console.WriteLine("注意: フレームレートはカメラから自動的に取得されるようになりました。この設定は無視されます。");
							i++;
							break;

						case "--codec":
							settings.VideoCodec = value.ToUpper();
							i++;
							break;

						case "--quality":
						case "-q":
							if (int.TryParse(value, out int quality) && quality >= 1 && quality <= 100)
							{
								settings.ImageQuality = quality;
							}
							i++;
							break;

						case "--format":
							settings.ImageFormat = value.ToLower();
							i++;
							break;

						case "--dir":
						case "-d":
							if (Directory.Exists(value) || Directory.Exists(Path.GetDirectoryName(value)))
							{
								settings.CameraSaveDirectory = value;
								Directory.CreateDirectory(value);
							}
							i++;
							break;

						case "--udpout":
							settings.UdpToAddress = value;
							i++;
							break;

						case "--udpin":
							settings.UdpListenAddress = value;
							i++;
							break;

						case "--udp":
							if (bool.TryParse(value, out bool udpFlag))
							{
								settings.UdpEnabled = udpFlag;
							}
							i++;
							break;

						case "--timeout":
						case "-t":
							if (int.TryParse(value, out int to))
							{
								settings.RecordingTimeoutMinutes = to;
								recordingTimeoutMinutes = to;
							}
							i++;
							break;

						case "--save":
						case "-s":
							// 設定を保存する場合
							if (value.ToLower() == "true" || value == "1")
							{
								settings.Save();
							}
							i++;
							break;
					}
				}
				else if (arg == "--help" || arg == "-h" || arg == "/?")
				{
					// ヘルプメッセージを表示
					ShowHelpMessage();
					Environment.Exit(0);
				}
				else if (arg == "--version" || arg == "-v")
				{
					// バージョン情報を表示
					MessageBox.Show($"Camera Recorder v1.0", "バージョン情報", MessageBoxButtons.OK, MessageBoxIcon.Information);
					Environment.Exit(0);
				}
			}
		}

		/// <summary>
		/// ヘルプメッセージを表示
		/// </summary>
		private static void ShowHelpMessage()
		{
			string helpMessage = @"カメラレコーダー コマンドライン引数:

基本設定:
  --com, -c <ポート名>        シリアルポートを指定 (例: COM1)
  --baud, -b <ボーレート>      ボーレートを指定 (例: 9600)
  --snap <文字列>             静止画トリガー文字列
  --starttrig <文字列>        録画開始トリガー文字列
  --stoptrig <文字列>         録画停止トリガー文字列
  --camera, -cam <インデックス> カメラインデックスを指定 (例: 0)
  --dir, -d <ディレクトリ>     録画・撮影データの保存先を指定

カメラ設定:  --resolution, -r <幅>x<高さ> 解像度を指定 (注: 現在は自動検出されるため無視されます)
  --fps, -f <フレームレート>   フレームレートを指定 (注: 現在は自動検出されるため無視されます)
  --codec <コーデック>         ビデオコーデックを指定 (例: H264, MJPG)
  --quality, -q <品質>         画質を指定 (1-100)
  --format <フォーマット>      静止画フォーマットを指定 (例: png, jpg)

ネットワーク設定:
  --udpout <IPアドレス:ポート> UDP送信先を指定 (例: 127.0.0.1:10000)
  --udpin <IPアドレス:ポート>  UDP受信設定を指定 (例: 127.0.0.1:10001)
  --udp <true|false>         UDP機能を有効/無効にする

その他設定:
  --timeout, -t <分>         録画停止トリガーのタイムアウト(分)

その他:
  --save, -s <true|false>      設定を保存するかどうか
  --help, -h, /?              このヘルプを表示
  --version, -v               バージョン情報を表示

例:
  triggerCam.exe --camera 1 --resolution 1920x1080 --fps 30 --save true
";
			MessageBox.Show(helpMessage, "コマンドライン引数ヘルプ", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private static void ForceStopRecording()
		{
			recordingTimeoutTimer?.Stop();
			recordingTimeoutTimer?.Dispose();
			recordingTimeoutTimer = null;

			if (cameraRecorder != null && cameraRecorder.IsRecording)
			{
				MessageBox.Show(
						"録画停止トリガーが一定時間内に検出されません。録画を強制終了します。",
						"録画強制終了",
						MessageBoxButtons.OK,
						MessageBoxIcon.Warning);

				SetRecordSource("timeout");
				cameraRecorder.StopRecording();
				trayIcon?.UpdateRecordingState(false);
			}
		}

		public static void StartRecordingTimeout()
		{
			StopRecordingTimeout();
			recordingTimeoutTimer = new System.Windows.Forms.Timer
			{
				Interval = recordingTimeoutMinutes * 60 * 1000
			};
			recordingTimeoutTimer.Tick += (s, e) => ForceStopRecording();
			recordingTimeoutTimer.Start();
		}

		public static void StopRecordingTimeout()
		{
			recordingTimeoutTimer?.Stop();
			recordingTimeoutTimer?.Dispose();
			recordingTimeoutTimer = null;
		}

		/// <summary>
		/// CameraRecorderのインスタンスを取得する
		/// </summary>
		/// <returns>CameraRecorderインスタンス</returns>
		public static CameraRecorder? GetCameraRecorder()
		{
			return cameraRecorder;
		}
	}
}
