using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using DirectShowLib;
using OpenCvSharp;
using triggerCam.Controls;
using triggerCam.Settings;

namespace triggerCam
{
	public partial class TrayIcon : Component
	{
		SynchronizationContext? uiContext;
		private AppSettings? settings;
		private string udpToIP = "127.0.0.1";
		private int udpToPort = 10000;
		private bool isRecording = false;
		private string lastRecordedFile = "";

		public TrayIcon()
		{
			InitializeComponent();
			Initialize();
		}

		public TrayIcon(IContainer container)
		{
			container.Add(this);
			InitializeComponent();
			Initialize();
		}
		private void Initialize()
		{
			uiContext = SynchronizationContext.Current;
			settings = AppSettings.Instance;

			context.Opened += ContextOpened;
			context.Closing += ContextClosing;
			context.Closed += (s, e) => { isContextMenuOpen = false; };
			contextMenu_comPortSelect.SelectedIndexChanged += OnSettingChanged;
			contextMenu_baudRateSelect.SelectedIndexChanged += OnSettingChanged;
			contextMenu_cameraSelect.SelectedIndexChanged += OnSettingChanged;
			contextMenu_modeContainer.SelectedIndexChanged += OnModeChanged;
			contextMenu_imageFormatContainer.SelectedIndexChanged += OnSettingChanged;
			contextMenu_udpSettings.AddressChanged += OnAddressChanged;
			contextMenu_udpSettings.UdpEnabledChanged += contextMenu_udpSettings_CheckedChanged;
			contextMenu_triggerStrings.SettingsChanged += OnSettingChanged;
			contextMenu_codecContainer.SelectedIndexChanged += OnSettingChanged;

			LoadSettings();

			// 初期状態でのモードに応じたボタン表示の更新
			UpdateButtonVisibility();
		}
		private void LoadSettings()
		{
			if (settings == null) return;
			// COMポート設定contextMenu_comPortSelect.Items.Clear();
			var ports = SerialPort.GetPortNames();
			if (ports.Length > 0)
			{
				contextMenu_comPortSelect.Items.AddRange(ports);
				if (ports.Contains(settings.ComPort))
					contextMenu_comPortSelect.SelectedItem = settings.ComPort;
				else
					contextMenu_comPortSelect.SelectedIndex = 0;
			}
			else
			{
				contextMenu_comPortSelect.Items.Add("シリアルポート");
				contextMenu_comPortSelect.SelectedIndex = 0;
			}

			// ボーレート設定
			contextMenu_baudRateSelect.Items.Clear();
			contextMenu_baudRateSelect.Items.AddRange(new object[] { "9600", "19200", "38400", "115200" });
			contextMenu_baudRateSelect.Text = settings.BaudRate.ToString();

			// カメラ選択の設定
			contextMenu_cameraSelect.Items.Clear();
			int selectedCameraIndex = settings.CameraIndex;
			bool foundSelectedCamera = false;

			// 実際のカメラ名を取得
			var cameraDevices = GetCameraDeviceList();
			foreach (var camera in cameraDevices)
			{
				contextMenu_cameraSelect.Items.Add(camera.Description);
				if (camera.Index == selectedCameraIndex)
				{
					contextMenu_cameraSelect.SelectedIndex = contextMenu_cameraSelect.Items.Count - 1;
					foundSelectedCamera = true;
				}
			}

			// 選択されているカメラが見つからない場合は単純にインデックスを追加
			if (!foundSelectedCamera && contextMenu_cameraSelect.Items.Count == 0)
			{
				contextMenu_cameraSelect.Items.Add(selectedCameraIndex.ToString());
				contextMenu_cameraSelect.SelectedIndex = 0;
			}

			// モード設定
			contextMenu_modeContainer.Items.Clear();
			contextMenu_modeContainer.Items.AddRange(new object[] { "静止画", "動画" });
			int modeIndex = (settings.CaptureMode == 0) ? 0 : 1;
			contextMenu_modeContainer.SelectedIndex = modeIndex;

			contextMenu_recordingsPath.Path = settings.CameraSaveDirectory;
			contextMenu_udpSettings.Address = settings.UdpToAddress;
			contextMenu_udpSettings.UdpEnabled = settings.UdpEnabled;
			contextMenu_triggerStrings.SnapTrigger = settings.SnapTrigger;
			contextMenu_triggerStrings.StartTrigger = settings.StartTrigger;
			contextMenu_triggerStrings.StopTrigger = settings.StopTrigger;

			// モードに応じたフォーマット設定の読み込みはUpdateButtonVisibilityで行う

			var addressParts = settings.UdpToAddress.Split(':');
			if (addressParts.Length == 2 && int.TryParse(addressParts[1], out int port))
			{
				udpToIP = addressParts[0];
				udpToPort = port;
			}

			// モードに応じた表示更新
			UpdateButtonVisibility();
		}

		public static bool isContextMenuOpen { get; private set; } = false;

		private void ContextOpened(object? sender, EventArgs e)
		{
			isContextMenuOpen = true;
                        contextMenu_save.Enabled = false;
                        RefreshDeviceLists();
                }

		private void ContextClosing(object? sender, ToolStripDropDownClosingEventArgs e)
		{
			// nothing special
		}

		private void trayIcon_MouseClick(object? sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (isContextMenuOpen) context.Close();
				else context.Show(Cursor.Position);
			}
		}

		private void OnSettingChanged(object? sender, EventArgs e)
		{
			contextMenu_save.Enabled = true;
		}

		private void OnAddressChanged(object? sender, EventArgs e)
		{
			if (settings != null && !string.Equals(contextMenu_udpSettings.Address, settings.UdpToAddress))
			{
				contextMenu_save.Enabled = true;
			}
		}

		private void OnRecordingsDirChanged(object? sender, EventArgs e)
		{
			if (settings != null && !string.Equals(contextMenu_recordingsPath.Path, settings.CameraSaveDirectory))
			{
				contextMenu_save.Enabled = true;
			}
		}
		private void contextMenu_save_Click(object sender, EventArgs e)
		{
			if (settings == null) return;
			string selectedPort = contextMenu_comPortSelect.SelectedItem as string ?? "";
			settings.ComPort = selectedPort == "シリアルポート" ? string.Empty : selectedPort;
			if (int.TryParse(contextMenu_baudRateSelect.Text, out int br))
				settings.BaudRate = br;

			// カメラインデックスを解析
			string cameraSelectText = contextMenu_cameraSelect.Text;
			int camIdx = 0;

			// カメラデバイスの一覧を取得
			var cameraDevices = GetCameraDeviceList();

			// 選択されたテキストに一致するカメラを探す
			var selectedCamera = cameraDevices.FirstOrDefault(c => c.Description == cameraSelectText);
			if (selectedCamera != null)
			{
				camIdx = selectedCamera.Index;
			}
			else if (cameraSelectText.Contains(':'))
			{
				// "インデックス: 名前" 形式からインデックスを抽出
				string idPart = cameraSelectText.Split(':')[0].Trim();
				if (int.TryParse(idPart, out int parsedId))
					camIdx = parsedId;
			}
			else if (int.TryParse(cameraSelectText, out int directId))
			{
				camIdx = directId;
			}

			settings.CameraIndex = camIdx;
			settings.CameraSaveDirectory = contextMenu_recordingsPath.Path;
			settings.UdpToAddress = contextMenu_udpSettings.Address;
			settings.UdpEnabled = contextMenu_udpSettings.UdpEnabled;
			settings.SnapTrigger = contextMenu_triggerStrings.SnapTrigger;
			settings.StartTrigger = contextMenu_triggerStrings.StartTrigger;
			settings.StopTrigger = contextMenu_triggerStrings.StopTrigger;
			// 現在のモードを保存
			settings.CaptureMode = contextMenu_modeContainer.SelectedIndex;

			// モードに応じた設定の保存
			if (contextMenu_modeContainer.SelectedIndex == 0) // 静止画モード
			{
				// 画像形式の保存
				settings.ImageFormat = contextMenu_imageFormatContainer.ComboBoxText.ToLower();
			}
			else // 動画モード
			{
				// ビデオコーデックの保存
				settings.VideoCodec = contextMenu_codecContainer.ComboBoxText.ToUpper();
			}

			settings.Save();
			Program.UpdateSerialSettings(settings);
			Program.UpdateCameraSettings(settings);

			var addressParts = settings.UdpToAddress.Split(':');
			if (addressParts.Length == 2 && int.TryParse(addressParts[1], out int port))
			{
				udpToIP = addressParts[0];
				udpToPort = port;
			}

                        contextMenu_save.Enabled = false;
                        RefreshDeviceLists();
                }

		private void contextMenu_browseRecordingsDir_Click(object sender, EventArgs e)
		{
			using var dialog = new FolderBrowserDialog();
			dialog.SelectedPath = contextMenu_recordingsPath.Path;
			if (dialog.ShowDialog() == DialogResult.OK)
			{
				contextMenu_recordingsPath.Path = dialog.SelectedPath;
			}
		}

		private void contextMenu_openRecordingsDir_Click(object sender, EventArgs e)
		{
			string path = contextMenu_recordingsPath.Path;
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
			Process.Start("explorer.exe", $"\"{path}\"");
		}

		private void contextMenu_refreshDevices_Click(object sender, EventArgs e)
		{
			RefreshDeviceLists();
		}
		private void contextMenu_exit_Click(object sender, EventArgs e)
		{
			// UDPServerを先に停止してから終了するようにプログラムを修正
			Program.CleanupBeforeExit();
			Application.Exit();
		}

		private void contextMenu_udpSettings_CheckedChanged(object? sender, EventArgs e)
		{
			Program.UpdateUdpEnabled(contextMenu_udpSettings.UdpEnabled);
			contextMenu_save.Enabled = true;
		}

		// Called from UDP CommandProcessor
		public void contextMenu_state_mute(bool mute)
		{
			// no-op in camera version
		}

		public void contextMenu_select_update(string[] devices)
		{
			uiContext?.Post(_ =>
			{
				contextMenu_cameraSelect.Items.Clear();
				contextMenu_cameraSelect.Items.AddRange(devices);
			}, null);
		}

		/// <summary>
		/// 外部から設定が変更された場合にUIを更新
		/// </summary>
		public void UpdateSettings()
		{
			if (uiContext == null || settings == null) return;

			// UIスレッドで実行
			uiContext.Post(_ =>
			{
				// 設定を再読み込み
				LoadSettings();

				// 保存ボタンを無効化（変更なし状態）
				contextMenu_save.Enabled = false;
			}, null);
		}

		/// <summary>
		/// カメラ録画の状態を更新
		/// </summary>
		/// <param name="isRec">録画中かどうか</param>
		public void UpdateRecordingState(bool isRec)
		{
			if (uiContext == null) return;

			// UIスレッドで実行
			uiContext.Post(_ =>
			{
				isRecording = isRec;

				// タスクトレイアイコンの状態を更新
				if (isRecording)
				{
					notifyIcon1.Text = "録画中...";
					contextMenu_recordingStatus.Text = "録画状態: 録画中";
					// アイコンを録画中のものに変更する場合はここで
				}
				else
				{
					notifyIcon1.Text = "待機中";
					contextMenu_recordingStatus.Text = "録画状態: 停止中";
					// アイコンを通常のものに戻す場合はここで
				}

				// ボタン表示も更新
				UpdateButtonVisibility();
			}, null);
		}        /// <summary>
						 /// モード変更時の処理
						 /// </summary>
		private void OnModeChanged(object? sender, EventArgs e)
		{
			// 保存ボタンを有効化
			contextMenu_save.Enabled = true;

			// モードに応じてUIを更新
			UpdateButtonVisibility();

			// CameraRecorderのモードも更新
			var cameraRecorder = Program.GetCameraRecorder();
			if (cameraRecorder != null)
			{
				cameraRecorder.CaptureMode = contextMenu_modeContainer.SelectedIndex;
			}

			// モード変更を通知
			Console.WriteLine($"モード変更: {(contextMenu_modeContainer.SelectedIndex == 0 ? "静止画" : "動画")}");
		}        /// <summary>
						 /// 選択されたモード（静止画/動画）に応じてUIの表示/非表示を切り替える
						 /// </summary>
		private void UpdateButtonVisibility()
		{
			// モードインデックス: 0=静止画、1=動画
			bool isImageMode = contextMenu_modeContainer.SelectedIndex == 0;
			// ボタンの表示切替
			contextMenu_triggerSnap.Visible = isImageMode;

			// 動画モードでは録画状態に応じてスタート/ストップボタンをトグルで表示
			if (!isImageMode)
			{
				contextMenu_triggerStart.Visible = !isRecording;  // 録画中でなければスタートボタンを表示
				contextMenu_triggerStop.Visible = isRecording;    // 録画中ならストップボタンを表示
			}
			else
			{
				// 静止画モードでは両方非表示
				contextMenu_triggerStart.Visible = false;
				contextMenu_triggerStop.Visible = false;
			}
			// コーデックコントロールとイメージフォーマットコントロールの表示/非表示を切り替え
			contextMenu_codecContainer.Visible = !isImageMode;
			contextMenu_imageFormatContainer.Visible = isImageMode;

			// フォーマット設定の表示を調整
			if (isImageMode)
			{
				// 静止画モードの場合は画像フォーマットを設定
				contextMenu_imageFormatContainer.ClearItems();
				contextMenu_imageFormatContainer.AddItems(new object[] { "PNG", "JPG", "BMP" });

				// 現在の設定を選択
				if (settings != null)
				{
					string format = settings.ImageFormat.ToUpper();
					bool found = false;
					foreach (var item in contextMenu_imageFormatContainer.Items)
					{
						if (item != null && item.ToString() == format)
						{
							contextMenu_imageFormatContainer.ComboBoxText = format;
							found = true;
							break;
						}
					}
					if (!found && contextMenu_imageFormatContainer.Items.Count > 0)
					{
						var firstItem = contextMenu_imageFormatContainer.GetItemAt(0);
						if (firstItem != null)
						{
							contextMenu_imageFormatContainer.ComboBoxText = firstItem.ToString() ?? "";
						}
					}
				}
			}
			else
			{
				// 動画モードの場合はコーデックを設定
				contextMenu_codecContainer.ClearItems();
				contextMenu_codecContainer.AddItems(new object[] { "H264", "MJPG", "MP4V" });

				// 現在の設定を選択
				if (settings != null)
				{
					string codec = settings.VideoCodec.ToUpper();
					bool found = false;
					foreach (var item in contextMenu_codecContainer.Items)
					{
						if (item != null && item.ToString() == codec)
						{
							contextMenu_codecContainer.ComboBoxText = codec;
							found = true;
							break;
						}
					}
					if (!found && contextMenu_codecContainer.Items.Count > 0)
					{
						var firstItem = contextMenu_codecContainer.GetItemAt(0);
						if (firstItem != null)
						{
							contextMenu_codecContainer.ComboBoxText = firstItem.ToString() ?? "";
						}
					}
				}
			}

			// カメラの現在の設定を取得してステータス表示を更新
			var cameraRecorder = Program.GetCameraRecorder();
			string cameraInfo = "";
			if (cameraRecorder != null)
			{
				var settings = cameraRecorder.GetSettings();
				if (settings.ContainsKey("resolution") && settings.ContainsKey("frameRate"))
				{
					cameraInfo = $" ({settings["resolution"]}, {settings["frameRate"]}fps)";
				}
			}

			// 録画状態ラベルの更新
			if (isImageMode)
			{
				contextMenu_recordingStatus.Text = "撮影モード：静止画" + cameraInfo;
			}
			else
			{
				contextMenu_recordingStatus.Text = (isRecording ? "撮影モード：録画（録画中）" : "撮影モード：録画（停止中）") + cameraInfo;
			}
		}

		/// <summary>
		/// SNAPボタンクリック時の処理
		/// </summary>
		private void contextMenu_triggerSnap_Click(object? sender, EventArgs e)
		{
			// CameraRecorderへのアクセス方法はプログラム構造に合わせて調整が必要
			var cameraRecorder = Program.GetCameraRecorder();
			if (cameraRecorder != null)
			{
				string fileName = Program.CreateFileName();
				Program.SetSnapshotSource("manual");
				cameraRecorder.TakeSnapshot(fileName);
			}
		}

		/// <summary>
		/// STARTボタンクリック時の処理
		/// </summary>
		private void contextMenu_triggerStart_Click(object? sender, EventArgs e)
		{
			// CameraRecorderへのアクセス方法はプログラム構造に合わせて調整が必要
			var cameraRecorder = Program.GetCameraRecorder();
			if (cameraRecorder != null && !isRecording)
			{
				string fileName = Program.CreateFileName();
				Program.SetRecordSource("manual");
				cameraRecorder.StartRecording(fileName);
				Program.Notify("manual", "RecStart");
				isRecording = true;
				UpdateRecordingState(true);
				Program.StartRecordingTimeout();
			}
		}

		/// <summary>
		/// STOPボタンクリック時の処理
		/// </summary>
		private void contextMenu_triggerStop_Click(object? sender, EventArgs e)
		{
			// CameraRecorderへのアクセス方法はプログラム構造に合わせて調整が必要
			var cameraRecorder = Program.GetCameraRecorder();
			if (cameraRecorder != null && isRecording)
			{
				Program.SetRecordSource("manual");
				cameraRecorder.StopRecording();
				isRecording = false;
				UpdateRecordingState(false);
				Program.StopRecordingTimeout();
			}
		}

		/// <summary>
		/// 現在選択されているモードを取得 (0:静止画, 1:動画)
		/// </summary>
		/// <returns>選択されているモードのインデックス</returns>
		public int GetSelectedMode()
		{
			return contextMenu_modeContainer.SelectedIndex;
		}

		/// <summary>
		/// 外部からモードを設定
		/// </summary>
		/// <param name="modeIndex">設定するモード (0:静止画, 1:動画)</param>
		public void SetMode(int modeIndex)
		{
			if (uiContext == null) return;

			// UIスレッドで実行
			uiContext.Post(_ =>
			{
				if (modeIndex >= 0 && modeIndex < contextMenu_modeContainer.Items.Count)
				{
					contextMenu_modeContainer.SelectedIndex = modeIndex;
					// OnModeChangedイベントが自動的に呼ばれる
				}
			}, null);
		}

                /// <summary>
                /// デバイス一覧を再取得してコンボボックスを更新
                /// </summary>
                public void RefreshDeviceLists()
                {
			if (uiContext == null) return;

			uiContext.Post(_ =>
			{
				// 現在の選択を保持
				string currentPort = contextMenu_comPortSelect.Text;
				string currentCamera = contextMenu_cameraSelect.Text;

				// Serial ports
				contextMenu_comPortSelect.Items.Clear();
				var ports = SerialPort.GetPortNames();
				if (ports.Length > 0)
				{
					contextMenu_comPortSelect.Items.AddRange(ports);
					int idx = Array.IndexOf(ports, currentPort);
					contextMenu_comPortSelect.SelectedIndex = idx >= 0 ? idx : 0;
				}
				else
				{
					contextMenu_comPortSelect.Items.Add("シリアルポート");
					contextMenu_comPortSelect.SelectedIndex = 0;
				}

				// Cameras
				contextMenu_cameraSelect.Items.Clear();
				var cameraDevices = GetCameraDeviceList();
				bool found = false;
				foreach (var cam in cameraDevices)
				{
					contextMenu_cameraSelect.Items.Add(cam.Description);
					if (cam.Description == currentCamera)
					{
						contextMenu_cameraSelect.SelectedIndex = contextMenu_cameraSelect.Items.Count - 1;
						found = true;
					}
				}
				if (!found && contextMenu_cameraSelect.Items.Count > 0)
				{
					contextMenu_cameraSelect.SelectedIndex = 0;
				}
			}, null);
		}

		/// <summary>
		/// システム内のすべてのカメラデバイスの名前と情報を取得します
		/// </summary>
		/// <returns>カメラデバイス情報のリスト</returns>
		private List<CameraDeviceInfo> GetCameraDeviceList()
		{
			var cameraList = new List<CameraDeviceInfo>();
			try
			{
				// DirectShowを使用してビデオデバイスを列挙
				var videoDevices = new List<DsDevice>(DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice));

				for (int i = 0; i < videoDevices.Count; i++)
				{
					var device = videoDevices[i];
					cameraList.Add(new CameraDeviceInfo
					{
						Index = i,
						Name = device.Name,
						Description = $"{i}: {device.Name}"
					});
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"カメラデバイス一覧の取得エラー: {ex.Message}");
				global::LogWriter.AddErrorLog(ex, "GetCameraDeviceList");
			}

			// デバイスが見つからない場合はダミーデータを作成
			if (cameraList.Count == 0)
			{
				// OpenCVで検出を試みる（名前はわからないがインデックスだけは取得可能）
				try
				{
					for (int i = 0; i < 10; i++)
					{
						using (var capture = new VideoCapture(i))
						{
							if (capture.IsOpened())
							{
								cameraList.Add(new CameraDeviceInfo
								{
									Index = i,
									Name = $"カメラ {i + 1}",
									Description = $"{i}: カメラ {i + 1}"
								});
							}
						}
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"OpenCVでのカメラ検出エラー: {ex.Message}");
					global::LogWriter.AddErrorLog(ex, "GetCameraDeviceList");
				}
			}

			return cameraList;
		}

		/// <summary>
		/// カメラデバイス情報を表すクラス
		/// </summary>
		private class CameraDeviceInfo
		{
			public int Index { get; set; }
			public string Name { get; set; } = "";
			public string Description { get; set; } = "";
		}
	}
}
