using System.Net.Sockets;
using System.Text.Json;
using triggerCam.Audio;
using triggerCam.Settings;

namespace triggerCam.UDP
{
    /// <summary>
    /// 外部からのコマンドを処理するクラス
    /// </summary>
    internal class CommandProcessor
    {
        private WatcherManager watcherManager;
        private AudioRecorder audioRecorder;
        private UdpClient udpClient;
        private string udpToIP;
        private int udpToPort;
        private triggerCam.TrayIcon? trayIcon;

        public CommandProcessor(WatcherManager watcherManager, UdpClient udpClient, string udpToIP, int udpToPort, AudioRecorder audioRecorder, triggerCam.TrayIcon? trayIcon)
        {
            this.watcherManager = watcherManager;
            this.udpClient = udpClient;
            this.udpToIP = udpToIP;
            this.udpToPort = udpToPort;
            this.audioRecorder = audioRecorder;
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

                    case "mic_on":
                        SetMicState(false);
                        SendResponse(new ResponseData { status = "success", message = "Microphone turned on" });
                        break;

                    case "mic_mute":
                        SetMicState(true);
                        SendResponse(new ResponseData { status = "success", message = "Microphone muted" });
                        break;

                    case "mic_status":
                        bool isMuted = IsMicMuted();
                        SendResponse(new ResponseData { 
                            status = "success", 
                            message = isMuted ? "Microphone is muted" : "Microphone is on",
                            data = new Dictionary<string, object> { { "isMuted", isMuted } }
                        });
                        break;

                    case "rec_start":
                        // 既に録音中でなければ開始
                        if (!IsRecording())
                        {
                            try
                            {
                                // パラメータをパース
                                string fileName;
                                string? customPath = null;
                                
                                if (!string.IsNullOrEmpty(command.param))
                                {
                                    // JSONとしてパースを試みる
                                    try
                                    {
                                        var paramObj = JsonSerializer.Deserialize<Dictionary<string, string>>(command.param);
                                        fileName = paramObj?.GetValueOrDefault("fileName") ?? DateTime.Now.ToString("yyyyMMdd_HHmmss");
                                        customPath = paramObj?.GetValueOrDefault("path");
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
                                
                                StartRecording(fileName, customPath);
                                SendResponse(new ResponseData { 
                                    status = "success", 
                                    message = "Recording started",
                                    data = new Dictionary<string, object> { 
                                        { "fileName", fileName },
                                        { "path", customPath ?? AppSettings.Instance.RecordingsDirectory }
                                    }
                                });
                                
                                // TrayIconの録音状態を更新
                                trayIcon?.UpdateRecordingState(true);
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
                        // 録音中であれば停止
                        if (IsRecording())
                        {
                            // パラメータをパース
                             bool saveToFile = false;
                             
                             if (!string.IsNullOrEmpty(command.param))
                             {
                                 // JSONとしてパースを試みる
                                 try
                                 {
                                     var paramObj = JsonSerializer.Deserialize<Dictionary<string, object>>(command.param);
                                     if (paramObj != null && paramObj.TryGetValue("saveToFile", out var saveToFileObj))
                                     {
                                         if (saveToFileObj is JsonElement element)
                                         {
                                             if (element.ValueKind == JsonValueKind.True)
                                                 saveToFile = true;
                                             else if (element.ValueKind == JsonValueKind.String && 
                                                      (element.GetString()?.ToLower() == "true" || element.GetString() == "1"))
                                                 saveToFile = true;
                                             else if (element.ValueKind == JsonValueKind.Number && element.GetInt32() == 1)
                                                 saveToFile = true;
                                         }
                                         else if (saveToFileObj is bool boolValue)
                                         {
                                             saveToFile = boolValue;
                                         }
                                         else if (saveToFileObj is string strValue)
                                         {
                                             saveToFile = strValue.ToLower() == "true" || strValue == "1";
                                         }
                                         else if (saveToFileObj is int intValue)
                                         {
                                             saveToFile = intValue == 1;
                                         }
                                     }
                                 }
                                 catch (Exception ex)
                                 {
                                     Console.WriteLine($"Error parsing rec_stop parameters: {ex.Message}");
                                     // パースエラーの場合はデフォルト値を使用
                                 }
                             }
                             
                             string filePath = StopRecording(saveToFile);
                            
                            if (string.IsNullOrEmpty(filePath))
                            {
                                // 音声が空の場合
                                SendResponse(new ResponseData { 
                                    status = "success", 
                                    message = "Recording stopped but audio was empty", 
                                    data = new Dictionary<string, object> { { "isEmpty", true } } 
                                });
                            }
                            else
                            {
                                // 音声がある場合
                                SendResponse(new ResponseData { 
                                    status = "success", 
                                    message = "Recording stopped", 
                                    data = new Dictionary<string, object> { 
                                         { "filePath", filePath }, 
                                         { "isEmpty", false },
                                         { "savedToFile", saveToFile }
                                     } 
                                });
                            }
                            
                            // TrayIconの録音状態を更新
                            trayIcon?.UpdateRecordingState(false, filePath);
                        }
                        else
                        {
                            SendResponse(new ResponseData { status = "error", message = "Not recording" });
                        }
                        break;

                    case "set_temp_file":
                         // 一時ファイル設定を変更
                         try
                         {
                             bool useTempFile = true; // デフォルト値
                             
                             if (!string.IsNullOrEmpty(command.param))
                             {
                                 // JSONとしてパースを試みる
                                 try
                                 {
                                     var paramObj = JsonSerializer.Deserialize<Dictionary<string, object>>(command.param);
                                     if (paramObj != null && paramObj.TryGetValue("useTempFile", out var useTempFileObj))
                                     {
                                         if (useTempFileObj is JsonElement element)
                                         {
                                             if (element.ValueKind == JsonValueKind.False)
                                                 useTempFile = false;
                                             else if (element.ValueKind == JsonValueKind.String && 
                                                      (element.GetString()?.ToLower() == "false" || element.GetString() == "0"))
                                                 useTempFile = false;
                                             else if (element.ValueKind == JsonValueKind.Number && element.GetInt32() == 0)
                                                 useTempFile = false;
                                         }
                                         else if (useTempFileObj is bool boolValue)
                                         {
                                             useTempFile = boolValue;
                                         }
                                         else if (useTempFileObj is string strValue)
                                         {
                                             useTempFile = !(strValue.ToLower() == "false" || strValue == "0");
                                         }
                                         else if (useTempFileObj is int intValue)
                                         {
                                             useTempFile = intValue != 0;
                                         }
                                     }
                                 }
                                 catch (Exception ex)
                                 {
                                     Console.WriteLine($"Error parsing set_temp_file parameters: {ex.Message}");
                                     // パースエラーの場合はデフォルト値を使用
                                 }
                             }
                             
                             // 設定を更新
                             audioRecorder.UpdateTempFileSettings(useTempFile);
                             
                             // TrayIconの一時ファイル設定を更新
                             trayIcon?.UpdateTempFileState(useTempFile);
                             
                             SendResponse(new ResponseData { 
                                 status = "success", 
                                 message = $"Temporary file setting updated", 
                                 data = new Dictionary<string, object> { { "useTempFile", useTempFile } } 
                             });
                         }
                         catch (Exception ex)
                         {
                             SendResponse(new ResponseData { status = "error", message = $"Failed to update temporary file setting: {ex.Message}" });
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

        /// <summary>
        /// マイクの状態を設定する
        /// </summary>
        /// <param name="mute">ミュート状態にするかどうか</param>
        private void SetMicState(bool mute)
        {
            if (watcherManager?.micWatcher?.monitorDevice != null)
            {
                try
                {
                    // 現在の状態を取得
                    bool currentState = watcherManager.micWatcher.monitorDevice.AudioEndpointVolume.Mute;
                    
                    // 状態を変更
                    watcherManager.micWatcher.monitorDevice.AudioEndpointVolume.Mute = mute;
                    
                    // 変更後の状態を確認
                    bool newState = watcherManager.micWatcher.monitorDevice.AudioEndpointVolume.Mute;
                    
                    Console.WriteLine($"マイク状態を変更: {(currentState ? "Mute" : "ON")} -> {(newState ? "Mute" : "ON")}");
                    
                    // 状態が変わらなかった場合は強制的に通知
                    if (currentState == newState && currentState != mute)
                    {
                        Console.WriteLine("マイク状態の変更に失敗したため、強制的に通知します");
                        trayIcon?.contextMenu_state_mute(mute);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"マイク状態の変更中にエラーが発生しました: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// マイクがミュート状態かどうかを取得する
        /// </summary>
        /// <returns>ミュート状態の場合はtrue</returns>
        private bool IsMicMuted()
        {
            if (watcherManager?.micWatcher?.monitorDevice != null)
            {
                return watcherManager.micWatcher.monitorDevice.AudioEndpointVolume.Mute;
            }
            return false;
        }

        // 録音状態
        private bool isRecording = false;
        
        /// <summary>
        /// 録音中かどうかを取得する
        /// </summary>
        /// <returns>録音中の場合はtrue</returns>
        private bool IsRecording()
        {
            return isRecording;
        }
        
        /// <summary>
        /// 録音を開始する
        /// </summary>
        /// <param name="fileName">録音ファイル名（拡張子なし）</param>
        /// <param name="customPath">カスタム保存パス（オプション）</param>
        private void StartRecording(string fileName, string? customPath = null)
        {
            if (watcherManager?.micWatcher?.monitorDevice != null && !isRecording)
            {
                audioRecorder.StartRecording(watcherManager.micWatcher.monitorDevice, fileName, customPath);
                isRecording = true;
            }
        }

        /// <summary>
        /// 録音を停止する
        /// </summary>
        /// <returns>録音ファイルのパス</returns>
        private string StopRecording(bool saveToFile = false)
        {
            if (isRecording)
            {
                string filePath = audioRecorder.StopRecording(saveToFile);
                isRecording = false;
                return filePath;
            }
            return "";
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
