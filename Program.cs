using System;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Runtime.InteropServices;
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
        private static string udpToIP = "127.0.0.1";
        private static int udpToPort = 10000;
        private static string udpListenIP = "127.0.0.1";
        private static int udpListenPort = 10001;

        [STAThread]
        static void Main(string[] args)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                MessageBox.Show(
                    "Serial features are only available on Windows.",
                    "Platform Not Supported",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }
            
            var settings = AppSettings.Instance;
            
            // コマンドライン引数の処理
            ProcessCommandLineArgs(args, settings);
            
            // UDP送信先の設定
            ParseUdpAddress(settings.UdpToAddress, out udpToIP, out udpToPort);
            
            // UDP受信の設定
            ParseUdpAddress(settings.UdpListenAddress, out udpListenIP, out udpListenPort);
            
            // UDPクライアントとサーバーの初期化
            udpClient = new UdpClient();
            udpServer = new UDPServer(udpListenIP, udpListenPort);
            
            // カメラレコーダーの初期化
            cameraRecorder = new CameraRecorder(settings.CameraIndex, settings.CameraSaveDirectory);
            cameraRecorder.SnapshotSaved += path => SendUdp($"{{ \"type\": \"snapshot\", \"path\": \"{path}\" }}");
            cameraRecorder.VideoSaved += path => SendUdp($"{{ \"type\": \"video_saved\", \"path\": \"{path}\" }}");
            
            // カメラをセットアップして実際の解像度とフレームレートを取得
            cameraRecorder.SetupCamera();

            // シリアルトリガーリスナーの初期化
            serialListener = new SerialTriggerListener(settings.ComPort, settings.BaudRate);
            serialListener.SnapReceived += () => cameraRecorder!.TakeSnapshot(CreateFileName());
            serialListener.StartReceived += () => cameraRecorder!.StartRecording(CreateFileName());
            serialListener.StopReceived += () => cameraRecorder!.StopRecording();

            serialListener.Start();

            ApplicationConfiguration.Initialize();
            
            // タスクトレイアイコンの初期化
            trayIcon = new TrayIcon();
            
            // コマンドプロセッサの初期化
            commandProcessor = new CommandProcessor(
                udpClient, 
                udpToIP, 
                udpToPort, 
                cameraRecorder, 
                trayIcon);
            
            // 定期的にUDPメッセージをチェック
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer
            {
                Interval = 100 // 100ミリ秒ごとにチェック
            };
            
            timer.Tick += (sender, e) =>
            {
                // キューからUDPデータを取得
                while (UDPServer_Static.q_UdpData.TryDequeue(out UDP_DATA? data))
                {
                    if (data != null)
                    {
                        commandProcessor.ProcessCommand(data);
                    }
                }
            };
            
            timer.Start();

            Application.ApplicationExit += (s, e) =>
            {
                timer.Stop();
                serialListener?.Dispose();
                cameraRecorder?.Dispose();
                udpClient?.Dispose();
                udpServer?.Dispose();
            };

            Application.Run();
        }

        private static void SendUdp(string message)
        {
            if (udpClient != null)
            {
                UDPSender.SendUDP(udpClient, message, udpToIP, udpToPort);
            }
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

                        case "--camera":
                        case "-cam":
                            if (int.TryParse(value, out int cameraIndex))
                            {
                                settings.CameraIndex = cameraIndex;
                            }
                            i++;
                            break;                        case "--resolution":
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

その他:
  --save, -s <true|false>      設定を保存するかどうか
  --help, -h, /?              このヘルプを表示
  --version, -v               バージョン情報を表示

例:
  triggerCam.exe --camera 1 --resolution 1920x1080 --fps 30 --save true
";
            MessageBox.Show(helpMessage, "コマンドライン引数ヘルプ", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
