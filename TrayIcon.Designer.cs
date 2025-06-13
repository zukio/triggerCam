// filepath: n:\data\WPF\triggerCam\TrayIcon.Designer.cs
using System.Windows.Forms;
using triggerCam.Controls;

namespace triggerCam
{
	partial class TrayIcon
	{
		/// <summary>
		/// 必要なデザイナー変数です。
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// 使用中のリソースをすべてクリーンアップします。
		/// </summary>
		/// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region コンポーネント デザイナーで生成されたコード

		/// <summary>
		/// デザイナー サポートに必要なメソッドです。このメソッドの内容を
		/// コード エディターで変更しないでください。
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TrayIcon));
			this.context = new ContextMenuStrip(this.components);
			this.contextMenu_serialContainer = new HorizontalMultiControlToolStripItem();
			this.contextMenu_cameraControlsContainer = new HorizontalMultiControlToolStripItem();
			this.contextMenu_recordingsDirLabel = new ToolStripMenuItem();
			this.contextMenu_recordingsPath = new RecordingPathToolStripItem();
                        this.contextMenu_openRecordingsDir = new ToolStripMenuItem();
                        this.contextMenu_udpSettings = new UdpSettingsToolStripItem();
			this.contextMenu_cameraSettingsSeparator = new ToolStripSeparator();
			this.contextMenu_imageFormatContainer = new HorizontalLayoutToolStripItem("画像形式:", 100);
			this.contextMenu_codecContainer = new HorizontalLayoutToolStripItem("動画コーデック:", 100);
			this.contextMenu_recordingStatus = new ToolStripMenuItem();
			this.contextMenu_save = new ToolStripMenuItem();
			this.contextMenu_exit = new ToolStripMenuItem();
			this.notifyIcon1 = new NotifyIcon(this.components);
			this.context.SuspendLayout();
			// 
			// context
			// 
			this.context.AccessibleRole = AccessibleRole.Cursor;
			this.context.BackColor = SystemColors.Window;
			this.context.ImageScalingSize = new Size(28, 28);
			this.context.Items.AddRange(new ToolStripItem[] {
																												this.contextMenu_serialContainer,
																												this.contextMenu_cameraControlsContainer,
																												this.contextMenu_recordingsDirLabel,
																												this.contextMenu_recordingsPath,
																												this.contextMenu_imageFormatContainer,
																												this.contextMenu_codecContainer,
                                                                               this.contextMenu_recordingStatus,
                                                                               this.contextMenu_openRecordingsDir,
                                                                               this.contextMenu_udpSettings,
                                                                               this.contextMenu_cameraSettingsSeparator,
																												this.contextMenu_save,
																												this.contextMenu_exit
																								});
			this.context.Name = "context";
			this.context.Size = new Size(332, 158);
			//
			//
			// contextMenu_serialContainer
			//
			this.contextMenu_serialContainer.Name = "contextMenu_serialContainer";
			this.contextMenu_comPortSelect = this.contextMenu_serialContainer.AddComboBox(120);
			this.contextMenu_comPortSelect.Name = "contextMenu_comPortSelect";
			this.contextMenu_comPortSelect.SelectedIndexChanged += OnSettingChanged;
			var labelBaud = this.contextMenu_serialContainer.AddLabel("ボーレート:");
			this.contextMenu_baudRateSelect = this.contextMenu_serialContainer.AddComboBox(100);
			this.contextMenu_baudRateSelect.Name = "contextMenu_baudRateSelect";
			this.contextMenu_baudRateSelect.SelectedIndexChanged += OnSettingChanged;
			//
			// contextMenu_cameraControlsContainer
			//
			this.contextMenu_cameraControlsContainer.Name = "contextMenu_cameraControlsContainer";

			// カメラコントロールコンテナに各コントロールを追加
			this.contextMenu_cameraSelect = this.contextMenu_cameraControlsContainer.AddComboBox(160);
			this.contextMenu_cameraSelect.Name = "contextMenu_cameraSelect";
			this.contextMenu_cameraSelect.SelectedIndexChanged += OnSettingChanged;
			//
			// contextMenu_modeContainer (ComboBox)
			//
			this.contextMenu_modeContainer = this.contextMenu_cameraControlsContainer.AddComboBox(100);
			this.contextMenu_modeContainer.Name = "contextMenu_modeContainer";
			this.contextMenu_modeContainer.Items.AddRange(new object[] { "静止画", "動画" });
			this.contextMenu_modeContainer.SelectedIndexChanged += OnModeChanged;
			this.contextMenu_triggerSnap = this.contextMenu_cameraControlsContainer.AddButton("静止画撮影", System.Drawing.Color.LightBlue);
			this.contextMenu_triggerSnap.Name = "contextMenu_triggerSnap";
			this.contextMenu_triggerSnap.Click += contextMenu_triggerSnap_Click;

			this.contextMenu_triggerStart = this.contextMenu_cameraControlsContainer.AddButton("録画開始", System.Drawing.Color.LightGreen);
			this.contextMenu_triggerStart.Name = "contextMenu_triggerStart";
			this.contextMenu_triggerStart.Click += contextMenu_triggerStart_Click;

			this.contextMenu_triggerStop = this.contextMenu_cameraControlsContainer.AddButton("録画停止", System.Drawing.Color.IndianRed);
			this.contextMenu_triggerStop.Name = "contextMenu_triggerStop";
			this.contextMenu_triggerStop.Click += contextMenu_triggerStop_Click;

			// 初期状態ですべてのボタンを表示する（表示/非表示はUpdateButtonVisibility()で制御）
			this.contextMenu_triggerSnap.Visible = true;
			this.contextMenu_triggerStart.Visible = true;
			this.contextMenu_triggerStop.Visible = false;

			//
			// contextMenu_recordingsDirLabel
			//
			this.contextMenu_recordingsDirLabel.Name = "contextMenu_recordingsDirLabel";
			this.contextMenu_recordingsDirLabel.Size = new Size(331, 36);
			this.contextMenu_recordingsDirLabel.Text = "撮影データの保存場所:";
			this.contextMenu_recordingsDirLabel.Enabled = false;
			// 
			// contextMenu_recordingsPath
			// 
			this.contextMenu_recordingsPath.Name = "contextMenu_recordingsPath";
			this.contextMenu_recordingsPath.Size = new Size(271, 35);
			this.contextMenu_recordingsPath.Path = "Recordings";
			this.contextMenu_recordingsPath.PathChanged += OnRecordingsDirChanged;
			this.contextMenu_recordingsPath.BrowseClicked += contextMenu_browseRecordingsDir_Click;
			//
			// contextMenu_cameraSettingsSeparator
			//
			this.contextMenu_cameraSettingsSeparator.Name = "contextMenu_cameraSettingsSeparator";
			this.contextMenu_cameraSettingsSeparator.Size = new Size(328, 6);
			//
			// contextMenu_imageFormatContainer
			//
			this.contextMenu_imageFormatContainer.Name = "contextMenu_imageFormatContainer";
			this.contextMenu_imageFormatContainer.LabelText = "画像形式:";
			this.contextMenu_imageFormatContainer.AddItems(new object[] { "PNG", "JPG" });
			this.contextMenu_imageFormatContainer.SelectedIndexChanged += OnSettingChanged;
			//
			// contextMenu_codecContainer
			//
			this.contextMenu_codecContainer.Name = "contextMenu_codecContainer";
			this.contextMenu_codecContainer.LabelText = "動画コーデック:";
			this.contextMenu_codecContainer.AddItems(new object[] { "H264", "MJPG", "WMV3" });
			this.contextMenu_codecContainer.SelectedIndexChanged += OnSettingChanged;
			//
			// contextMenu_recordingStatus
			//
			this.contextMenu_recordingStatus.Name = "contextMenu_recordingStatus";
			this.contextMenu_recordingStatus.Size = new Size(331, 36);
			this.contextMenu_recordingStatus.Text = "録画状態: 停止中";
			this.contextMenu_recordingStatus.Enabled = false;
			// 
			// contextMenu_openRecordingsDir
			// 
                        this.contextMenu_openRecordingsDir.Name = "contextMenu_openRecordingsDir";
                        this.contextMenu_openRecordingsDir.Size = new Size(331, 36);
                        this.contextMenu_openRecordingsDir.Text = "保存済データを開く";
                        this.contextMenu_openRecordingsDir.Click += contextMenu_openRecordingsDir_Click;
                        //
                        // contextMenu_udpSettings
                        this.contextMenu_udpSettings.Name = "contextMenu_udpSettings";
                        this.contextMenu_udpSettings.Size = new Size(271, 30);
                        // contextMenu_save
                        //
                        this.contextMenu_save.Enabled = false;
			this.contextMenu_save.Name = "contextMenu_save";
			this.contextMenu_save.Size = new Size(331, 36);
			this.contextMenu_save.Text = "設定を保存";
			this.contextMenu_save.Click += contextMenu_save_Click;
			// 
			// contextMenu_exit
			// 
			this.contextMenu_exit.Name = "contextMenu_exit";
			this.contextMenu_exit.Size = new Size(331, 36);
			this.contextMenu_exit.Text = "Exit";
			this.contextMenu_exit.Click += contextMenu_exit_Click;
			// 
			// notifyIcon1
			// 
			this.notifyIcon1.ContextMenuStrip = this.context;
			this.notifyIcon1.Icon = (Icon)resources.GetObject("notifyIcon1.Icon");
			this.notifyIcon1.Text = "Camera Recorder";
			this.notifyIcon1.Visible = true;
			this.notifyIcon1.MouseClick += trayIcon_MouseClick;
			this.context.ResumeLayout(false);
		}

		#endregion

		private ContextMenuStrip context;
		private HorizontalMultiControlToolStripItem contextMenu_serialContainer;
		private ComboBox contextMenu_comPortSelect;
		private ComboBox contextMenu_baudRateSelect;
		private HorizontalMultiControlToolStripItem contextMenu_cameraControlsContainer;
		private Button contextMenu_triggerSnap;
		private Button contextMenu_triggerStart;
		private Button contextMenu_triggerStop;
		private ComboBox contextMenu_cameraSelect;
		private ComboBox contextMenu_modeContainer;
		private ToolStripMenuItem contextMenu_save;
		private ToolStripMenuItem contextMenu_exit;
		private NotifyIcon notifyIcon1;
		private ToolStripMenuItem contextMenu_recordingsDirLabel;
                private RecordingPathToolStripItem contextMenu_recordingsPath;
                private ToolStripMenuItem contextMenu_openRecordingsDir;
                private UdpSettingsToolStripItem contextMenu_udpSettings;
                private ToolStripSeparator contextMenu_cameraSettingsSeparator;
		private ToolStripMenuItem contextMenu_recordingStatus;
		private HorizontalLayoutToolStripItem contextMenu_imageFormatContainer;
		private HorizontalLayoutToolStripItem contextMenu_codecContainer;
	}
}
