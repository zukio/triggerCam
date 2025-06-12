﻿using System.Windows.Forms;
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
            this.contextMenu_comPort = new ToolStripComboBox();
            this.contextMenu_baudRate = new ToolStripComboBox();
            this.contextMenu_triggerSnap = new ToolStripButton();
            this.contextMenu_triggerStart = new ToolStripButton();
            this.contextMenu_triggerStop = new ToolStripButton();
            this.contextMenu_cameraSelect = new ToolStripComboBox();
            this.contextMenu_mode = new ToolStripComboBox();            this.contextMenu_recordingsDirLabel = new ToolStripMenuItem();
            this.contextMenu_recordingsPath = new RecordingPathToolStripItem();
            this.contextMenu_openRecordingsDir = new ToolStripMenuItem();
            this.contextMenu_address = new ToolStripTextBox();            
            this.contextMenu_cameraSettingsSeparator = new ToolStripSeparator();
            this.contextMenu_imageFormatLabel = new ToolStripMenuItem();
            this.contextMenu_imageFormat = new ToolStripComboBox();
            this.contextMenu_codecLabel = new ToolStripMenuItem();
            this.contextMenu_codec = new ToolStripComboBox();
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
            this.context.ImageScalingSize = new Size(28, 28);              this.context.Items.AddRange(new ToolStripItem[] {
                                                        this.contextMenu_comPort,
                                                        this.contextMenu_baudRate,
                                                        this.contextMenu_triggerSnap,
                                                        this.contextMenu_triggerStart,
                                                        this.contextMenu_triggerStop,
                                                        this.contextMenu_cameraSelect,
                                                        this.contextMenu_mode,
                                                        this.contextMenu_recordingsDirLabel,
                                                        this.contextMenu_recordingsPath,
                                                        this.contextMenu_openRecordingsDir,                                                        this.contextMenu_address,
                                                        this.contextMenu_cameraSettingsSeparator,
                                                        this.contextMenu_imageFormatLabel,
                                                        this.contextMenu_imageFormat,
                                                        this.contextMenu_codecLabel,
                                                        this.contextMenu_codec,
                                                        this.contextMenu_recordingStatus,
                                                        this.contextMenu_save,
                                                        this.contextMenu_exit
                                                });
            this.context.Name = "context";
            this.context.Size = new Size(332, 158);
            // 
            // contextMenu_comPort
            //
            this.contextMenu_comPort.Name = "contextMenu_comPort";
            this.contextMenu_comPort.Size = new Size(160, 38);
            this.contextMenu_comPort.Text = "COM";
            //
            // contextMenu_baudRate
            //
            this.contextMenu_baudRate.Name = "contextMenu_baudRate";
            this.contextMenu_baudRate.Size = new Size(100, 38);
            this.contextMenu_baudRate.Items.AddRange(new object[] { "9600", "19200", "38400", "115200" });
            // 
            // contextMenu_triggerSnap
            //
            this.contextMenu_triggerSnap.Name = "contextMenu_triggerSnap";
            this.contextMenu_triggerSnap.Size = new Size(160, 34);
            this.contextMenu_triggerSnap.Text = "静止画撮影";
            this.contextMenu_triggerSnap.DisplayStyle = ToolStripItemDisplayStyle.Text;
            this.contextMenu_triggerSnap.BackColor = System.Drawing.Color.LightBlue;
            this.contextMenu_triggerSnap.Click += contextMenu_triggerSnap_Click;
            //
            // contextMenu_triggerStart
            //
            this.contextMenu_triggerStart.Name = "contextMenu_triggerStart";
            this.contextMenu_triggerStart.Size = new Size(160, 34);
            this.contextMenu_triggerStart.Text = "録画開始";
            this.contextMenu_triggerStart.DisplayStyle = ToolStripItemDisplayStyle.Text;
            this.contextMenu_triggerStart.BackColor = System.Drawing.Color.LightGreen;
            this.contextMenu_triggerStart.Click += contextMenu_triggerStart_Click;
            //
            // contextMenu_triggerStop
            //
            this.contextMenu_triggerStop.Name = "contextMenu_triggerStop";
            this.contextMenu_triggerStop.Size = new Size(160, 34);
            this.contextMenu_triggerStop.Text = "録画停止";
            this.contextMenu_triggerStop.DisplayStyle = ToolStripItemDisplayStyle.Text;
            this.contextMenu_triggerStop.BackColor = System.Drawing.Color.IndianRed;
            this.contextMenu_triggerStop.Click += contextMenu_triggerStop_Click;
            //
            // contextMenu_cameraSelect
            //
            this.contextMenu_cameraSelect.Name = "contextMenu_cameraSelect";
            this.contextMenu_cameraSelect.Size = new Size(160, 38);
            //
            // contextMenu_mode
            //
            this.contextMenu_mode.Name = "contextMenu_mode";
            this.contextMenu_mode.Size = new Size(121, 38);
            this.contextMenu_mode.Items.AddRange(new object[] { "静止画", "動画" });
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
            // contextMenu_openRecordingsDir
            // 
            this.contextMenu_openRecordingsDir.Name = "contextMenu_openRecordingsDir";
            this.contextMenu_openRecordingsDir.Size = new Size(331, 36);
            this.contextMenu_openRecordingsDir.Text = "撮影データを開く";
            this.contextMenu_openRecordingsDir.Click += contextMenu_openRecordingsDir_Click;
            // 
            // contextMenu_address
            // 
            this.contextMenu_address.AutoCompleteCustomSource.AddRange(new string[] { "127.0.0.1:10000" });
            this.contextMenu_address.Name = "contextMenu_address";
            this.contextMenu_address.Size = new Size(271, 35);
            this.contextMenu_address.Text = "127.0.0.1:10000";            //
            // contextMenu_cameraSettingsSeparator
            //
            this.contextMenu_cameraSettingsSeparator.Name = "contextMenu_cameraSettingsSeparator";
            this.contextMenu_cameraSettingsSeparator.Size = new Size(328, 6);
            //
            // contextMenu_imageFormat
            //
            this.contextMenu_imageFormat.Name = "contextMenu_imageFormat";
            this.contextMenu_imageFormat.Size = new Size(100, 38);
            this.contextMenu_imageFormat.Text = "画像形式";
            this.contextMenu_imageFormat.Items.AddRange(new object[] { "PNG", "JPG" });
            this.contextMenu_imageFormat.SelectedIndexChanged += OnSettingChanged;            //
            // contextMenu_imageFormatLabel
            //
            this.contextMenu_imageFormatLabel.Name = "contextMenu_imageFormatLabel";
            this.contextMenu_imageFormatLabel.Size = new Size(331, 36);
            this.contextMenu_imageFormatLabel.Text = "画像形式:";
            this.contextMenu_imageFormatLabel.Enabled = false;
            //
            // contextMenu_codecLabel
            //
            this.contextMenu_codecLabel.Name = "contextMenu_codecLabel";
            this.contextMenu_codecLabel.Size = new Size(331, 36);
            this.contextMenu_codecLabel.Text = "動画コーデック:";
            this.contextMenu_codecLabel.Enabled = false;
            //
            // contextMenu_codec
            //
            this.contextMenu_codec.Name = "contextMenu_codec";
            this.contextMenu_codec.Size = new Size(100, 38);
            this.contextMenu_codec.Text = "コーデック";
            this.contextMenu_codec.Items.AddRange(new object[] { "H264", "MJPG", "WMV3" });
            this.contextMenu_codec.SelectedIndexChanged += OnSettingChanged;
            //
            // contextMenu_recordingStatus
            //
            this.contextMenu_recordingStatus.Name = "contextMenu_recordingStatus";
            this.contextMenu_recordingStatus.Size = new Size(331, 36);
            this.contextMenu_recordingStatus.Text = "録画状態: 停止中";
            this.contextMenu_recordingStatus.Enabled = false;
            // 
            // contextMenu_save
            // 
            this.contextMenu_save.Enabled = false;
            this.contextMenu_save.Name = "contextMenu_save";
            this.contextMenu_save.Size = new Size(331, 36);
            this.contextMenu_save.Text = "保存";
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
        private ToolStripComboBox contextMenu_comPort;
        private ToolStripComboBox contextMenu_baudRate;
        private ToolStripButton contextMenu_triggerSnap;
        private ToolStripButton contextMenu_triggerStart;
        private ToolStripButton contextMenu_triggerStop;
        private ToolStripComboBox contextMenu_cameraSelect;
        private ToolStripComboBox contextMenu_mode;
        private ToolStripMenuItem contextMenu_save;
        private ToolStripMenuItem contextMenu_exit;
        private NotifyIcon notifyIcon1;
				private ToolStripTextBox contextMenu_address;
        private ToolStripMenuItem contextMenu_recordingsDirLabel;
        private RecordingPathToolStripItem contextMenu_recordingsPath;
        private ToolStripMenuItem contextMenu_openRecordingsDir;        
				private ToolStripSeparator contextMenu_cameraSettingsSeparator;
        private ToolStripComboBox contextMenu_imageFormat;
        private ToolStripMenuItem contextMenu_recordingStatus;
        private ToolStripMenuItem contextMenu_imageFormatLabel;
        private ToolStripComboBox contextMenu_codec;
        private ToolStripMenuItem contextMenu_codecLabel;
    }
}
