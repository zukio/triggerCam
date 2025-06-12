using System.Net.Sockets;
using micNotifyUDP.Audio;
using micNotifyUDP.LogWriter;
using micNotifyUDP.Settings;
using micNotifyUDP.UDP;

namespace micNotify
{
    internal static class Program
    {
        // オーディオの状態を監視するクラス
        static WatcherManager? watcher;

        // 二重起動管理のため、ミューテックスの名前（特異な名前を指定）
        private static string mutexName = "AudioWatcher";

        static string UDP_toIP = "127.0.0.1";
        static int UDP_toPort = 10000;
        
        // UDPサーバー（コマンド受信用）
        static UDPServer? udpServer;
        static CommandProcessor? commandProcessor;
        
        // コマンド処理用スレッド
        static Thread? commandThread;
        static bool isRunning = true;
        
        // 録音機能
        static AudioRecorder? audioRecorder;
        
        // 録音状態
        static bool isRecording = false;
        static TrayIcon? trayIconInstance = null;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // 二重起動管理
            bool createdNew;
            // Mutexのインスタンスを作成
            using (Mutex mutex = new Mutex(true, mutexName, out createdNew))
            {
                if (createdNew) // このアプリケーションのインスタンスが初めて起動された場合
                {
                    // デバッガー：Console.WriteLine()をログ出力
                    SaveConsole logfile = SaveConsole.Instance;

                    // 引数に false を渡すとGDI+を使用せずにテキストを描画、true を渡すとGDI+を使用してテキストを描画
                    Application.SetCompatibleTextRenderingDefault(false);
                    // To customize application configuration such as set high DPI settings or default font,
                    // see https://aka.ms/applicationconfiguration.
                    ApplicationConfiguration.Initialize();

                    // 設定の読み込み
                    var settings = AppSettings.Instance;
                    
                    // 環境変数（起動引数）の取得（設定よりも優先）
                    string deviceName = getEnv(args, "/deviceName") ?? settings.DeviceName;
                    string udpTo = getEnv(args, "/udpTo") ?? settings.UdpToAddress;
                    string udpListen = getEnv(args, "/udpListen") ?? settings.UdpListenAddress;
                    string recordingsDir = getEnv(args, "/recordingsDir");
                    if (!string.IsNullOrEmpty(recordingsDir))
                    {
                        settings.RecordingsDirectory = recordingsDir;
                        settings.Save();
                    }

                    // ヘルパー：文字列で渡されたUDPアドレス（0.0.0.0:00000）をIPアドレスおよびポート番号に分解し有効な値を設定
                    UdpAddressChanger udpAddressChanger = new UdpAddressChanger(udpTo);
                    UDP_toIP = udpAddressChanger.ip;
                    UDP_toPort = udpAddressChanger.port;

                    // コマンド受信用UDPサーバーの設定
                    UdpAddressChanger udpListenAddressChanger = new UdpAddressChanger(udpListen);
                    string udpListenIP = udpListenAddressChanger.ip;
                    int udpListenPort = udpListenAddressChanger.port;

                    // 通信用UdpClient インスタンスを作成
                    UdpClient udpClient = new UdpClient();
                    Action<string?> udpConnect = stringValue => { UDPSender.SendUDP(udpClient, (stringValue != null) ? $"Connected {stringValue}" : "disConnected", UDP_toIP, UDP_toPort); };
                    Action<bool> udpMute = boolValue => { UDPSender.SendUDP(udpClient, boolValue == true ? "isMute" : "ON", UDP_toIP, UDP_toPort); };
                    Action<string?> udpChange = stringValue => { UDPSender.SendUDP(udpClient, (stringValue != null) ? $"Changed {stringValue}" : "null", UDP_toIP, UDP_toPort); };

                   // 一時ファイルの使用有無を起動時引数から取得
										string useTempFile = getEnv(args, "/useTempFile");
										if (!string.IsNullOrEmpty(useTempFile))
										{
												if (bool.TryParse(useTempFile, out bool tempFileEnabled))
												{
														settings.UseTempFile = tempFileEnabled;
														settings.Save();
														Console.WriteLine($"一時ファイル機能を{(tempFileEnabled ? "有効" : "無効")}に設定しました");
												}
										}

                    // UIの生成
                    trayIconInstance = new TrayIcon();  // ここでInitializeComponentが呼ばれる

                    // オーディオの状態を監視するクラスを生成
                    watcher = new WatcherManager(deviceName);
                    if (watcher.micWatcher == null) return;

                    // オーディオの状態が変化したらUIに伝達
                    watcher.micWatcher.scanHook += trayIconInstance.contextMenu_select_update;
                    watcher.micWatcher.connectHook += trayIconInstance.contextMenu_state_connect;
                    watcher.micWatcher.muteHook += trayIconInstance.contextMenu_state_mute;
                    watcher.micWatcher.connectHook += udpConnect;
                    watcher.micWatcher.muteHook += udpMute;

                    watcher.Initialize();

                    // UIにてユーザーが監視対象を変更した時
                    trayIconInstance.reStart += reStart;
                    trayIconInstance.reStart += udpChange;
                    trayIconInstance.reStart += logfile.WriteLog;
                    trayIconInstance.changeUdpAddress += changeUdpAddress;
                    trayIconInstance.setDevice(watcher.micWatcher.targetDeviceName);

                    // 最初のログに情報を記録
                    logfile.WriteLog(watcher.micWatcher.targetDeviceName);

                    // 録音機能を初期化
                    audioRecorder = new AudioRecorder();
                    
                    // TrayIconに録音機能を設定
                    trayIconInstance.SetAudioRecorder(audioRecorder);
                    
                    // マイク操作と録音のイベントハンドラを設定
                    trayIconInstance.toggleMic += ToggleMicState;
                    trayIconInstance.startRecording += StartRecording;
                    trayIconInstance.stopRecording += StopRecording;
                    trayIconInstance.thresholdChanged += UpdateThresholdSettings;

                    // コマンド受信用UDPサーバーを初期化
                    udpServer = new UDPServer(udpListenIP, udpListenPort);
                    commandProcessor = new CommandProcessor(watcher, udpClient, UDP_toIP, UDP_toPort, audioRecorder, trayIconInstance);

                    // コマンド処理用スレッドを開始
                    commandThread = new Thread(ProcessCommands);
                    commandThread.IsBackground = true;
                    commandThread.Start();

                    // アプリケーションが終了するときにリソース解放
                    Application.ApplicationExit += (sender, e) =>
                    {
                        // 録音中なら停止
                        if (isRecording)
                        {
                            StopRecording();
                        }
                        
                        // コマンド処理スレッドを停止
                        isRunning = false;
                        commandThread?.Join(1000);

                        if (trayIconInstance != null)
                        {
                            if (watcher != null)
                            {
                                watcher.micWatcher.scanHook -= trayIconInstance.contextMenu_select_update;
                                watcher.micWatcher.connectHook -= trayIconInstance.contextMenu_state_connect;
                                watcher.micWatcher.muteHook -= trayIconInstance.contextMenu_state_mute;
                            }
                            trayIconInstance.reStart -= reStart;
                            trayIconInstance.reStart -= udpChange;
                            trayIconInstance.reStart -= logfile.WriteLog;
                            trayIconInstance.changeUdpAddress -= changeUdpAddress;
                            trayIconInstance.toggleMic -= ToggleMicState;
                            trayIconInstance.startRecording -= StartRecording;
                            trayIconInstance.stopRecording -= StopRecording;
                            trayIconInstance.thresholdChanged -= UpdateThresholdSettings;
                            trayIconInstance.Dispose();
                            trayIconInstance = null;
                        }
                        
                        // 録音機能を解放
                        audioRecorder?.Dispose();
                        audioRecorder = null;

                        if (watcher != null)
                        {
                            watcher.micWatcher.connectHook -= udpConnect;
                            watcher.micWatcher.muteHook -= udpMute;
                            watcher.Dispose();
                            watcher = null;
                        }

                        // UDPサーバーを解放
                        udpServer?.Dispose();
                        udpServer = null;

                        // UdpClient インスタンス解放
                        udpClient.Dispose();

                        logfile.Dispose();
                    };
                    
                    // 現在のOSのテーマに従って描画 // このメソッドは Application.Run の前に呼び出す必要があります。
                    Application.EnableVisualStyles();
                    
                    Application.Run();
                }
                else // このアプリケーションのインスタンスが既に起動している場合
                {
                    Console.WriteLine("二重起動がブロックされました");
                    MessageBox.Show("既に起動しています", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        /// <summary>
        /// コマンド処理スレッドのメインループ
        /// </summary>
        private static void ProcessCommands()
        {
            while (isRunning)
            {
                try
                {
                    // キューからデータを取得
                    if (UDPServer_Static.q_UdpData.TryDequeue(out UDP_DATA? data) && data != null)
                    {
                        // コマンドを処理
                        commandProcessor?.ProcessCommand(data);
                    }
                    else
                    {
                        // データがない場合は少し待機
                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing command: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 環境変数（起動引数）の取得
        /// </summary>
        private static string? getEnv(string[] args, string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                // 起動引数からAPIキーを取得
                // string[] args = Environment.GetCommandLineArgs();
                foreach (string arg in args)
                {
                    string[] split = arg.Split('=');
                    if (string.Equals(split[0].ToLower(), key.ToLower()))
                    {
                        if (split.Length > 1)
                        {
                            return split[1];
                        }
                        break;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// UIにてユーザーが監視対象を変更した時に監視プログラムを再起動
        ///  <param name="deviceName">>監視したいデバイス（マイクなど）を検索するためのキーワード（デバイス名など）</param>
        /// </summary>
        private static void reStart(string deviceName)
        {
            watcher?.micWatcher?.reStart(deviceName);
            
            // 設定を保存
            var settings = AppSettings.Instance;
            settings.DeviceName = deviceName;
            settings.Save();
        }

        static void changeUdpAddress(string newAddress)
        {
            // ヘルパー：文字列で渡されたUDPアドレス（0.0.0.0:00000）をIPアドレスおよびポート番号に分解し有効な値を設定
            UdpAddressChanger udpAddressChanger = new UdpAddressChanger(newAddress);
            UDP_toIP = udpAddressChanger.ip;
            UDP_toPort = udpAddressChanger.port;
            
            // 設定を保存
            var settings = AppSettings.Instance;
            settings.UdpToAddress = newAddress;
            settings.Save();
        }

        /// <summary>
        /// マイクの状態を切り替える
        /// </summary>
        /// <param name="mute">ミュート状態にするかどうか</param>
        private static void ToggleMicState(bool mute)
        {
            if (watcher?.micWatcher?.monitorDevice != null)
            {
                try
                {
                    // 現在の状態を取得
                    bool currentState = watcher.micWatcher.monitorDevice.AudioEndpointVolume.Mute;
                    
                    // 状態を変更
                    watcher.micWatcher.monitorDevice.AudioEndpointVolume.Mute = mute;
                    
                    // 変更後の状態を確認
                    bool newState = watcher.micWatcher.monitorDevice.AudioEndpointVolume.Mute;
                    
                    Console.WriteLine($"マイク状態を変更: {(currentState ? "Mute" : "ON")} -> {(newState ? "Mute" : "ON")}");
                    
                    // 状態が変わらなかった場合は強制的に通知
                    if (currentState == newState && currentState != mute)
                    {
                        Console.WriteLine("マイク状態の変更に失敗したため、強制的に通知します");
                        trayIconInstance?.contextMenu_state_mute(mute);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"マイク状態の変更中にエラーが発生しました: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 録音を開始する
        /// </summary>
        /// <param name="fileName">録音ファイル名（拡張子なし）</param>
        private static void StartRecording(string? fileName)
        {
            if (watcher?.micWatcher?.monitorDevice != null && audioRecorder != null && !isRecording)
            {
                try
                {
                    // 録音開始
                    string actualFileName = fileName ?? DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    audioRecorder.StartRecording(watcher.micWatcher.monitorDevice, actualFileName);
                    Console.WriteLine($"録音開始: {actualFileName}");
                    
                    // 録音状態を更新
                    isRecording = true;
                    
                    // TrayIconの録音状態を更新
                    trayIconInstance?.UpdateRecordingState(true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"録音開始エラー: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 録音を停止する
        /// </summary>
        /// <param name="saveToFile">一時ファイルを永続的なファイルに保存するかどうか</param>
         private static void StopRecording(bool saveToFile = false)
        {
            if (audioRecorder != null && isRecording)
            {
                try
                {
                    // 録音停止
										string filePath = audioRecorder.StopRecording(saveToFile);
                    Console.WriteLine($"録音停止: {filePath}");
                    
                    // 録音状態を更新
                    isRecording = false;
                    
                    // TrayIconの録音状態を更新
                    trayIconInstance?.UpdateRecordingState(false, filePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"録音停止エラー: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 閾値設定を更新する
        /// </summary>
        /// <param name="threshold">新しい閾値</param>
        /// <param name="minSamples">新しい最小サンプル数</param>
        private static void UpdateThresholdSettings(int threshold, int minSamples)
        {
            if (audioRecorder != null)
            {
                audioRecorder.UpdateThresholdSettings(threshold, minSamples);
                Console.WriteLine($"閾値設定を更新: Threshold={threshold}, MinSamples={minSamples}");
            }
        }
    }
}
