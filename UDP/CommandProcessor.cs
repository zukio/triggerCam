using System.Net.Sockets;
using System.Text.Json;
using triggerCam.Camera;
using triggerCam.Settings;

namespace triggerCam.UDP
{
    /// <summary>
    /// 外部からのコマンドを処理するクラス
    /// </summary>
    internal class CommandProcessor
    {
        private CameraRecorder cameraRecorder;
        private UdpClient udpClient;
        private string udpToIP;
        private int udpToPort;
        private triggerCam.TrayIcon? trayIcon;

        public CommandProcessor(UdpClient udpClient, string udpToIP, int udpToPort, CameraRecorder cameraRecorder, triggerCam.TrayIcon? trayIcon)
        {
            this.cameraRecorder = cameraRecorder;
            this.udpClient = udpClient;
            this.udpToIP = udpToIP;
            this.udpToPort = udpToPort;
            this.trayIcon = trayIcon;
        }

        /// <summary>
        /// 受信したコマンドを処理する
        /// </summary>
        /// <param name="data">受信したUDPデータ</param>
        public void ProcessCommand(UDP_DATA data)
        {
            try
            {
                // 受信したデータを表示
                Console.WriteLine($"Received raw data: {data.rcvString}");
                
                // 文字列を正規化（全角括弧を半角に変換など）
                string normalizedJson = NormalizeJsonString(data.rcvString);
                Console.WriteLine($"Normalized JSON: {normalizedJson}");
                
                // JSONとしてパース
                var options = new JsonSerializerOptions
                {
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };
                
                var command = JsonSerializer.Deserialize<CommandData>(normalizedJson, options);
                if (command == null)
                {
                    Console.WriteLine("Invalid command format");
                    return;
                }

                Console.WriteLine($"Received command: {command.command}, params: {command.param}");

                switch (command.command.ToLower())
                {
                    case "exit":
                        SendResponse(new ResponseData { status = "success", message = "Application will exit" });
                        Application.Exit();
                        break;

                    case "take_snapshot":
                        try
                        {
                            // パラメータをパース
                            string fileName;
                            
                            if (!string.IsNullOrEmpty(command.param))
                            {
                                // JSONとしてパースを試みる
                                try
                                {
                                    var paramObj = JsonSerializer.Deserialize<Dictionary<string, string>>(command.param);
                                    fileName = paramObj?.GetValueOrDefault("fileName") ?? DateTime.Now.ToString("yyyyMMdd_HHmmss");
                                }
                                catch
                                {
                                    // JSONパースに失敗した場合は、パラメータ全体をファイル名として扱う
                                    fileName = command.param;
                                }
                            }
                            else
                            {
                                fileName = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                            }
                            
                            cameraRecorder.TakeSnapshot(fileName);
                            SendResponse(new ResponseData { 
                                status = "success", 
                                message = "Snapshot taken",
                                data = new Dictionary<string, object> { 
                                    { "fileName", fileName }
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            SendResponse(new ResponseData { status = "error", message = $"Failed to take snapshot: {ex.Message}" });
                        }
                        break;

                    case "rec_start":
                        // 既に録画中でなければ開始
                        if (!IsRecording())
                        {
                            try
                            {
                                // パラメータをパース
                                string fileName;
                                
                                if (!string.IsNullOrEmpty(command.param))
                                {
                                    // JSONとしてパースを試みる
                                    try
                                    {
                                        var paramObj = JsonSerializer.Deserialize<Dictionary<string, string>>(command.param);
                                        fileName = paramObj?.GetValueOrDefault("fileName") ?? DateTime.Now.ToString("yyyyMMdd_HHmmss");
                                    }
                                    catch
                                    {
                                        // JSONパースに失敗した場合は、パラメータ全体をファイル名として扱う
                                        fileName = command.param;
                                    }
                                }
                                else
                                {
                                    fileName = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                                }
                                
                                StartRecording(fileName);
                                SendResponse(new ResponseData { 
                                    status = "success", 
                                    message = "Recording started",
                                    data = new Dictionary<string, object> { 
                                        { "fileName", fileName }
                                    }
                                });
                                
                                // TrayIconの録画状態を更新（TrayIconにメソッドがある場合）
                                // trayIcon?.UpdateRecordingState(true);
                            }
                            catch (Exception ex)
                            {
                                SendResponse(new ResponseData { status = "error", message = $"Failed to start recording: {ex.Message}" });
                            }
                        }
                        else
                        {
                            SendResponse(new ResponseData { status = "error", message = "Already recording" });
                        }
                        break;

                    case "rec_stop":
                        // 録画中であれば停止
                        if (IsRecording())
                        {
                            StopRecording();
                            
                            SendResponse(new ResponseData { 
                                status = "success", 
                                message = "Recording stopped"
                            });
                            
                            // TrayIconの録画状態を更新（TrayIconにメソッドがある場合）
                            // trayIcon?.UpdateRecordingState(false);
                        }
                        else
                        {
                            SendResponse(new ResponseData { status = "error", message = "Not recording" });
                        }
                        break;

                    default:
                        SendResponse(new ResponseData { status = "error", message = $"Unknown command: {command.command}" });
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing command: {ex.Message}");
                SendResponse(new ResponseData { status = "error", message = $"Error: {ex.Message}" });
            }
        }

        // 録画状態
        private bool isRecording = false;
        
        /// <summary>
        /// 録画中かどうかを取得する
        /// </summary>
        /// <returns>録画中の場合はtrue</returns>
        private bool IsRecording()
        {
            return isRecording;
        }
        
        /// <summary>
        /// 録画を開始する
        /// </summary>
        /// <param name="fileName">録画ファイル名（拡張子なし）</param>
        private void StartRecording(string fileName)
        {
            if (!isRecording)
            {
                cameraRecorder.StartRecording(fileName);
                isRecording = true;
            }
        }

        /// <summary>
        /// 録画を停止する
        /// </summary>
        private void StopRecording()
        {
            if (isRecording)
            {
                cameraRecorder.StopRecording();
                isRecording = false;
            }
        }

        /// <summary>
        /// レスポンスを送信する
        /// </summary>
        /// <param name="response">レスポンスデータ</param>
        private void SendResponse(ResponseData response)
        {
            string jsonResponse = JsonSerializer.Serialize(response);
            UDPSender.SendUDP(udpClient, jsonResponse, udpToIP, udpToPort);
        }
        
        /// <summary>
        /// JSON文字列を正規化する（全角括弧を半角に変換、BOMを除去など）
        /// </summary>
        /// <param name="input">入力JSON文字列</param>
        /// <returns>正規化されたJSON文字列</returns>
        private string NormalizeJsonString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
                
            // BOMマーカーを除去
            if (input.Length > 0 && input[0] == 0xFEFF)
                input = input.Substring(1);
                
            // 全角括弧を半角に変換
            input = input.Replace('｛', '{').Replace('｝', '}');
            input = input.Replace('［', '[').Replace('］', ']');
            input = input.Replace('"', '"').Replace('"', '"');
            input = input.Replace('：', ':').Replace('，', ',');
            
            // 先頭の不正な文字を除去
            int startIndex = 0;
            while (startIndex < input.Length && !IsValidJsonStart(input[startIndex]))
                startIndex++;
                
            if (startIndex >= input.Length)
                return "{}"; // 有効なJSON文字がない場合は空のオブジェクトを返す
                
            return input.Substring(startIndex);
        }
        
        /// <summary>
        /// 文字がJSON開始に有効かどうかをチェック
        /// </summary>
        private bool IsValidJsonStart(char c)
        {
            return c == '{' || c == '[' || c == '"' || char.IsWhiteSpace(c);
        }
    }

    /// <summary>
    /// コマンドデータ
    /// </summary>
    internal class CommandData
    {
        public string command { get; set; } = "";
        public string? param { get; set; } = null;
    }

    /// <summary>
    /// レスポンスデータ
    /// </summary>
    internal class ResponseData
    {
        public string status { get; set; } = "success";
        public string message { get; set; } = "";
        public Dictionary<string, object>? data { get; set; } = null;
    }
}
