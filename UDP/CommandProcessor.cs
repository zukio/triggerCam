using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text.Json;
using System.Windows.Forms;
using System.IO.Ports;
using DirectShowLib;
using triggerCam.Camera;
using triggerCam.Settings;
using triggerCam;

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
		// モード定数
		private const int MODE_IMAGE = 0;
		private const int MODE_VIDEO = 1;

		// 録画状態
		private bool isRecording = false;

		public CommandProcessor(UdpClient udpClient, string udpToIP, int udpToPort, CameraRecorder cameraRecorder, triggerCam.TrayIcon? trayIcon)
		{
			this.cameraRecorder = cameraRecorder;
			this.udpClient = udpClient;
			this.udpToIP = udpToIP;
			this.udpToPort = udpToPort;
			this.trayIcon = trayIcon;

			// TrayIconからモードを取得して、CameraRecorderにも設定
			if (trayIcon != null)
			{
				cameraRecorder.CaptureMode = trayIcon.GetSelectedMode();
			}
		}

		/// <summary>
		/// 現在選択されているモードを取得
		/// </summary>
		/// <returns>モード (0=静止画, 1=動画)</returns>
		private int GetCurrentMode()
		{
			return trayIcon?.GetSelectedMode() ?? MODE_VIDEO; // デフォルトは動画モード
		}
		/// <summary>
		/// コマンドが現在のモードで実行可能かチェック
		/// </summary>
		/// <param name="command">コマンド名</param>
		/// <returns>実行可能ならtrue、そうでなければfalse</returns>
		private bool IsCommandAllowedInCurrentMode(string command)
		{
			int currentMode = GetCurrentMode();

			switch (command.ToLower())
			{
				case "take_snapshot":
					return currentMode == MODE_IMAGE;

				case "rec_start":
				case "rec_stop":
					return currentMode == MODE_VIDEO;

				case "set_format":
					return currentMode == MODE_IMAGE;

				case "set_codec":
					return currentMode == MODE_VIDEO;

				// 他のコマンドはモード非依存
				default:
					return true;
			}
		}

		/// <summary>
		/// モードに依存するエラーメッセージを取得
		/// </summary>
		/// <param name="command">コマンド名</param>
		/// <returns>エラーメッセージ</returns>
		private string GetModeErrorMessage(string command)
		{
			switch (command.ToLower())
			{
				case "take_snapshot":
					return "静止画撮影は「静止画モード」でのみ実行できます。モードを変更してください。";

				case "rec_start":
				case "rec_stop":
					return "録画操作は「動画モード」でのみ実行できます。モードを変更してください。";

				case "set_format":
					return "画像フォーマットの設定は「静止画モード」でのみ実行できます。モードを変更してください。";

				case "set_codec":
					return "動画コーデックの設定は「動画モード」でのみ実行できます。モードを変更してください。";

				default:
					return "このコマンドは現在のモードでは実行できません。";
			}
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
				string raw = $"Received raw data: {data.rcvString}";
				Console.WriteLine(raw);
				global::LogWriter.AddLog(raw);

				// 文字列を正規化（全角括弧を半角に変換など）
				string normalizedJson = NormalizeJsonString(data.rcvString);
				string normalizedLog = $"Normalized JSON: {normalizedJson}";
				Console.WriteLine(normalizedLog);
				global::LogWriter.AddLog(normalizedLog);

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
					global::LogWriter.AddLog("Invalid command format");
					return;
				}

				string cmdLog = $"Received command: {command.command}, params: {command.param}";
				Console.WriteLine(cmdLog);
				global::LogWriter.AddLog(cmdLog);

				// 現在のモードでコマンドが許可されているかチェック
				if (!IsCommandAllowedInCurrentMode(command.command))
				{
					SendResponse(new ResponseData
					{
						status = "error",
						message = GetModeErrorMessage(command.command),
						data = new Dictionary<string, object> {
														{ "currentMode", GetCurrentMode() == MODE_IMAGE ? "静止画モード" : "動画モード" }
												}
					});
					return;
				}

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
									fileName = paramObj?.GetValueOrDefault("fileName") ?? Program.CreateFileName();
								}
								catch
								{
									// JSONパースに失敗した場合は、パラメータ全体をファイル名として扱う
									fileName = command.param;
								}
							}
							else
							{
								fileName = Program.CreateFileName();
							}
							Program.SetSnapshotSource("success");

							// パラメータからカスタムパスを取得
							string? customPath = null;
							if (!string.IsNullOrEmpty(command.param))
							{
								try
								{
									var paramObj = JsonSerializer.Deserialize<Dictionary<string, string>>(command.param);
									customPath = paramObj?.GetValueOrDefault("customPath");
								}
								catch
								{
									// JSONパースエラーの場合はカスタムパスなし
								}
							}

							// 実際のファイルパスを計算
							var settings = cameraRecorder.GetSettings();
							string extension = settings["imageFormat"].ToString() ?? "png";
							// 拡張子の正規化
							if (extension != "jpg" && extension != "jpeg" && extension != "png" && extension != "bmp")
							{
								extension = "png"; // デフォルト
							}

							// 保存ディレクトリを取得（カスタムパスが指定されていればそちらを優先）
							string saveDir = customPath ?? AppSettings.Instance.CameraSaveDirectory;
							// ディレクトリが存在しない場合は作成
							if (!string.IsNullOrEmpty(saveDir) && !Directory.Exists(saveDir))
							{
								Directory.CreateDirectory(saveDir);
							}


							// CameraRecorderクラスでVideoSavedイベントが発火し、実際のファイルパスを含む通知が送信されます
							// string fullPath = Path.Combine(saveDir, fileName + "." + extension);
							//SendResponse(new ResponseData
							//{
							//	status = "success",
							//	message = "SnapSaved",
							//	data = new Dictionary<string, object> {
							//		{ "path", fullPath }
							//	}
							//});
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
										fileName = paramObj?.GetValueOrDefault("fileName") ?? Program.CreateFileName();
									}
									catch
									{
										// JSONパースに失敗した場合は、パラメータ全体をファイル名として扱う
										fileName = command.param;
									}
								}
								else
								{
									fileName = Program.CreateFileName();
								}
								Program.SetRecordSource("success");

								// パラメータからカスタムパスを取得
								string? customPath = null;
								if (!string.IsNullOrEmpty(command.param))
								{
									try
									{
										var paramObj = JsonSerializer.Deserialize<Dictionary<string, string>>(command.param);
										customPath = paramObj?.GetValueOrDefault("customPath");
									}
									catch
									{
										// JSONパースエラーの場合はカスタムパスなし
									}
								}

								// 実際のファイルパスを計算
								string saveDir = customPath ?? AppSettings.Instance.CameraSaveDirectory;
								// ディレクトリが存在しない場合は作成
								if (!string.IsNullOrEmpty(saveDir) && !Directory.Exists(saveDir))
								{
									Directory.CreateDirectory(saveDir);
								}
								string fullPath = Path.Combine(saveDir, fileName + ".mp4");

								StartRecording(fileName, customPath);
								Program.Notify("success", "RecStart");
								Program.StartRecordingTimeout();
								// CameraRecorderクラスでVideoSavedイベントが発火し、実際のファイルパスを含む通知が送信されます
								//SendResponse(new ResponseData
								//{
								//	status = "success",
								//	message = "RecStart",
								//	data = new Dictionary<string, object> {
								//		{ "path", fullPath },
								//	}
								//});

								// TrayIconの録画状態を更新
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
						// 録画中であれば停止
						if (IsRecording())
						{
							Program.SetRecordSource("success");

							// 録画を停止（VideoSavedイベントが発生する）
							StopRecording();
							Program.StopRecordingTimeout();

							// CameraRecorderクラスでVideoSavedイベントが発火し、実際のファイルパスを含む通知が送信されます
							//SendResponse(new ResponseData
							//{
							//	status = "success",
							//	message = "RecStop"
							//});

							// TrayIconの録画状態を更新
							trayIcon?.UpdateRecordingState(false);
						}
						else
						{
							SendResponse(new ResponseData { status = "error", message = "Not recording" });
						}
						break;

					case "set_resolution":
						try
						{
							// 解像度設定コマンドは現在サポートされていません
							// 実際のカメラ解像度を取得して返します

							// カメラから現在の設定を取得
							var currentSettings = cameraRecorder.GetSettings();
							string resolution = currentSettings["resolution"].ToString() ?? "unknown";

							SendResponse(new ResponseData
							{
								status = "info",
								message = "解像度は自動的に取得されるため設定できません",
								data = new Dictionary<string, object> {
									{ "resolution", resolution },
									{ "note", "カメラの実際の解像度が自動的に使用されます" }
								}
							});
						}
						catch (Exception ex)
						{
							SendResponse(new ResponseData { status = "error", message = $"解像度取得エラー: {ex.Message}" });
						}
						break;

					case "set_framerate":
						try
						{
							// フレームレート設定コマンドは現在サポートされていません
							// 実際のカメラフレームレートを取得して返します

							// カメラから現在の設定を取得
							var currentSettings = cameraRecorder.GetSettings();
							int fps = Convert.ToInt32(currentSettings["frameRate"]);

							SendResponse(new ResponseData
							{
								status = "info",
								message = "フレームレートは自動的に取得されるため設定できません",
								data = new Dictionary<string, object> {
									{ "frameRate", fps },
									{ "note", "カメラの実際のフレームレートが自動的に使用されます" }
								}
							});
						}
						catch (Exception ex)
						{
							SendResponse(new ResponseData { status = "error", message = $"フレームレート取得エラー: {ex.Message}" });
						}
						break;

					case "set_codec":
						try
						{
							if (string.IsNullOrEmpty(command.param))
							{
								SendResponse(new ResponseData { status = "error", message = "Missing codec parameter" });
								break;
							}

							string codec = command.param.ToUpper();
							// カメラ設定を更新
							cameraRecorder.SetVideoCodec(codec);

							// AppSettingsも更新
							var settings = AppSettings.Instance;
							settings.VideoCodec = codec;

							// 設定を保存
							settings.Save();

							// TrayIconのUIを更新
							trayIcon?.UpdateSettings();

							SendResponse(new ResponseData
							{
								status = "success",
								message = "Video codec updated",
								data = new Dictionary<string, object> {
									{ "codec", codec }
								}
							});
						}
						catch (Exception ex)
						{
							SendResponse(new ResponseData { status = "error", message = $"Failed to set codec: {ex.Message}" });
						}
						break;

					case "set_quality":
						try
						{
							if (string.IsNullOrEmpty(command.param) || !int.TryParse(command.param, out int quality))
							{
								SendResponse(new ResponseData { status = "error", message = "Invalid quality value" });
								break;
							}

							if (quality < 1 || quality > 100)
							{
								SendResponse(new ResponseData { status = "error", message = "Quality must be between 1 and 100" });
								break;
							}

							// カメラ設定を更新
							cameraRecorder.SetImageQuality(quality);

							// AppSettingsも更新
							var settings = AppSettings.Instance;
							settings.ImageQuality = quality;

							// 設定を保存
							settings.Save();

							// TrayIconのUIを更新
							trayIcon?.UpdateSettings();

							SendResponse(new ResponseData
							{
								status = "success",
								message = "Image quality updated",
								data = new Dictionary<string, object> {
									{ "quality", quality }
								}
							});
						}
						catch (Exception ex)
						{
							SendResponse(new ResponseData { status = "error", message = $"Failed to set quality: {ex.Message}" });
						}
						break;

					case "set_format":
						try
						{
							if (string.IsNullOrEmpty(command.param))
							{
								SendResponse(new ResponseData { status = "error", message = "Missing format parameter" });
								break;
							}

							string format = command.param.ToLower();
							if (format != "jpg" && format != "jpeg" && format != "png" && format != "bmp")
							{
								SendResponse(new ResponseData { status = "error", message = "Supported formats: jpg, jpeg, png, bmp" });
								break;
							}

							// カメラ設定を更新
							cameraRecorder.SetImageFormat(format);

							// AppSettingsも更新
							var settings = AppSettings.Instance;
							settings.ImageFormat = format;

							// 設定を保存
							settings.Save();

							// TrayIconのUIを更新
							trayIcon?.UpdateSettings();

							SendResponse(new ResponseData
							{
								status = "success",
								message = "Image format updated",
								data = new Dictionary<string, object> {
									{ "format", format }
								}
							});
						}
						catch (Exception ex)
						{
							SendResponse(new ResponseData { status = "error", message = $"Failed to set format: {ex.Message}" });
						}
						break;

                                        case "get_settings":
                                                try
                                                {
                                                        // UIのデバイスリストも更新
                                                        trayIcon?.RefreshDeviceLists();

                                                        // 現在のカメラ設定を取得
                                                        var currentSettings = cameraRecorder.GetSettings();

                                                        // デバイスリストを取得
                                                        string[] ports = Array.Empty<string>();
                                                        try { ports = SerialPort.GetPortNames(); } catch { }

                                                        var cameraNames = new List<string>();
                                                        try
                                                        {
                                                                var videoDevices = new List<DsDevice>(DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice));
                                                                for (int i = 0; i < videoDevices.Count; i++)
                                                                {
                                                                        cameraNames.Add($"{i}: {videoDevices[i].Name}");
                                                                }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                                global::LogWriter.AddErrorLog(ex, "GetCameraDeviceList");
                                                        }

                                                        currentSettings["serialPorts"] = ports;
                                                        currentSettings["cameraDevices"] = cameraNames;

                                                        SendResponse(new ResponseData
                                                        {
                                                                status = "success",
                                                                message = "Camera settings",
                                                                data = currentSettings
                                                        });
                                                }
                                                catch (Exception ex)
                                                {
                                                        SendResponse(new ResponseData { status = "error", message = $"Failed to get settings: {ex.Message}" });
                                                }
                                                break;

					case "set_mode":
						try
						{
							if (string.IsNullOrEmpty(command.param))
							{
								SendResponse(new ResponseData { status = "error", message = "Missing mode parameter" });
								break;
							}

							string mode = command.param.ToLower();
							int modeIndex;

							if (mode == "image" || mode == "静止画" || mode == "0")
							{
								modeIndex = MODE_IMAGE;
							}
							else if (mode == "video" || mode == "動画" || mode == "1")
							{
								modeIndex = MODE_VIDEO;
							}
							else
							{
								SendResponse(new ResponseData { status = "error", message = "Invalid mode. Use 'image' or 'video'" });
								break;
							}

							// モードを更新
							if (trayIcon != null)
							{
								trayIcon.SetMode(modeIndex);

								// CameraRecorderのモードも更新
								cameraRecorder.CaptureMode = modeIndex;

								// 設定を保存
								var settings = AppSettings.Instance;
								settings.CaptureMode = modeIndex;
								settings.Save();

								SendResponse(new ResponseData
								{
									status = "success",
									message = "Mode updated",
									data = new Dictionary<string, object> {
										{ "mode", modeIndex == MODE_IMAGE ? "image" : "video" }
									}
								});
							}
							else
							{
								SendResponse(new ResponseData { status = "error", message = "TrayIcon not available" });
							}
						}
						catch (Exception ex)
						{
							SendResponse(new ResponseData { status = "error", message = $"Failed to set mode: {ex.Message}" });
						}
						break;

					default:
						SendResponse(new ResponseData { status = "error", message = $"Unknown command: {command.command}" });
						break;
				}
			}
			catch (Exception ex)
			{
				string errLog = $"Error processing command: {ex.Message}";
				Console.WriteLine(errLog);
				global::LogWriter.AddLog(errLog);
				global::LogWriter.AddErrorLog(ex, nameof(ProcessCommand));
				SendResponse(new ResponseData { status = "error", message = $"Error: {ex.Message}" });
			}
		}

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
		/// <param name="customPath">カスタム保存ディレクトリ（指定された場合）</param>
		private void StartRecording(string fileName, string? customPath = null)
		{
			if (!isRecording)
			{
				cameraRecorder.StartRecording(fileName, customPath);
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
