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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TrayIcon));
            context = new ContextMenuStrip(components);
            contextMenu_comPort = new ToolStripComboBox();
            contextMenu_baudRate = new ToolStripComboBox();
            contextMenu_triggerSnap = new ToolStripTextBox();
            contextMenu_triggerStart = new ToolStripTextBox();
            contextMenu_triggerStop = new ToolStripTextBox();
            contextMenu_cameraSelect = new ToolStripComboBox();
            contextMenu_mode = new ToolStripComboBox();
            contextMenu_recordingsDirLabel = new ToolStripMenuItem();
            contextMenu_recordingsPath = new RecordingPathToolStripItem();
            contextMenu_openRecordingsDir = new ToolStripMenuItem();
            contextMenu_address = new ToolStripTextBox();
            contextMenu_save = new ToolStripMenuItem();
            contextMenu_exit = new ToolStripMenuItem();
            notifyIcon1 = new NotifyIcon(components);
            context.SuspendLayout();
            // 
            // context
            // 
            context.AccessibleRole = AccessibleRole.Cursor;
            context.BackColor = SystemColors.Window;
            context.ImageScalingSize = new Size(28, 28);
            context.Items.AddRange(new ToolStripItem[] {
                                                        contextMenu_comPort,
                                                        contextMenu_baudRate,
                                                        contextMenu_triggerSnap,
                                                        contextMenu_triggerStart,
                                                        contextMenu_triggerStop,
                                                        contextMenu_cameraSelect,
                                                        contextMenu_mode,
                                                        contextMenu_recordingsDirLabel,
                                                        contextMenu_recordingsPath,
                                                        contextMenu_openRecordingsDir,
                                                        contextMenu_address,
                                                        contextMenu_save,
                                                        contextMenu_exit
                                                });
            context.Name = "context";
            context.Size = new Size(332, 158);
            // 
            // contextMenu_comPort
            //
            contextMenu_comPort.Name = "contextMenu_comPort";
            contextMenu_comPort.Size = new Size(160, 38);
            contextMenu_comPort.Text = "COM";
            //
            // contextMenu_baudRate
            //
            contextMenu_baudRate.Name = "contextMenu_baudRate";
            contextMenu_baudRate.Size = new Size(100, 38);
            contextMenu_baudRate.Items.AddRange(new object[] { "9600", "19200", "38400", "115200" });
            //
            // contextMenu_triggerSnap
            //
            contextMenu_triggerSnap.Name = "contextMenu_triggerSnap";
            contextMenu_triggerSnap.Size = new Size(160, 34);
            contextMenu_triggerSnap.Text = "SNAP";
            //
            // contextMenu_triggerStart
            //
            contextMenu_triggerStart.Name = "contextMenu_triggerStart";
            contextMenu_triggerStart.Size = new Size(160, 34);
            contextMenu_triggerStart.Text = "START";
            //
            // contextMenu_triggerStop
            //
            contextMenu_triggerStop.Name = "contextMenu_triggerStop";
            contextMenu_triggerStop.Size = new Size(160, 34);
            contextMenu_triggerStop.Text = "STOP";
            //
            // contextMenu_cameraSelect
            //
            contextMenu_cameraSelect.Name = "contextMenu_cameraSelect";
            contextMenu_cameraSelect.Size = new Size(160, 38);
            //
            // contextMenu_mode
            //
            contextMenu_mode.Name = "contextMenu_mode";
            contextMenu_mode.Size = new Size(121, 38);
            contextMenu_mode.Items.AddRange(new object[] { "静止画", "動画" });
            //
            // contextMenu_recordingsDirLabel
            //
            contextMenu_recordingsDirLabel.Name = "contextMenu_recordingsDirLabel";
            contextMenu_recordingsDirLabel.Size = new Size(331, 36);
            contextMenu_recordingsDirLabel.Text = "撮影データの保存場所:";
            contextMenu_recordingsDirLabel.Enabled = false;
            // 
            // contextMenu_recordingsPath
            // 
            contextMenu_recordingsPath.Name = "contextMenu_recordingsPath";
            contextMenu_recordingsPath.Size = new Size(271, 35);
            contextMenu_recordingsPath.Path = "Recordings";
            contextMenu_recordingsPath.PathChanged += OnRecordingsDirChanged;
            contextMenu_recordingsPath.BrowseClicked += contextMenu_browseRecordingsDir_Click;
            // 
            // contextMenu_openRecordingsDir
            // 
            contextMenu_openRecordingsDir.Name = "contextMenu_openRecordingsDir";
            contextMenu_openRecordingsDir.Size = new Size(331, 36);
            contextMenu_openRecordingsDir.Text = "撮影データを開く";
            contextMenu_openRecordingsDir.Click += contextMenu_openRecordingsDir_Click;
            // 
            // contextMenu_address
            // 
            contextMenu_address.AutoCompleteCustomSource.AddRange(new string[] { "127.0.0.1:10000" });
            contextMenu_address.Name = "contextMenu_address";
            contextMenu_address.Size = new Size(271, 35);
            contextMenu_address.Text = "127.0.0.1:10000";
            // 
            // contextMenu_save
            // 
            contextMenu_save.Enabled = false;
            contextMenu_save.Name = "contextMenu_save";
            contextMenu_save.Size = new Size(331, 36);
            contextMenu_save.Text = "保存";
            contextMenu_save.Click += contextMenu_save_Click;
            // 
            // contextMenu_exit
            // 
            contextMenu_exit.Name = "contextMenu_exit";
            contextMenu_exit.Size = new Size(331, 36);
            contextMenu_exit.Text = "Exit";
            contextMenu_exit.Click += contextMenu_exit_Click;
            // 
            // notifyIcon1
            // 
            notifyIcon1.ContextMenuStrip = context;
            notifyIcon1.Icon = (Icon)resources.GetObject("notifyIcon1.Icon");
            notifyIcon1.Text = "AudioWatcher";
            notifyIcon1.Visible = true;
            notifyIcon1.MouseClick += trayIcon_MouseClick;
            context.ResumeLayout(false);
        }

        #endregion

        private ContextMenuStrip context;
        private ToolStripComboBox contextMenu_comPort;
        private ToolStripComboBox contextMenu_baudRate;
        private ToolStripTextBox contextMenu_triggerSnap;
        private ToolStripTextBox contextMenu_triggerStart;
        private ToolStripTextBox contextMenu_triggerStop;
        private ToolStripComboBox contextMenu_cameraSelect;
        private ToolStripComboBox contextMenu_mode;
        private ToolStripMenuItem contextMenu_save;
        private ToolStripMenuItem contextMenu_exit;
        private NotifyIcon notifyIcon1;
        private ToolStripTextBox contextMenu_address;
        private ToolStripMenuItem contextMenu_recordingsDirLabel;
        private RecordingPathToolStripItem contextMenu_recordingsPath;
        private ToolStripMenuItem contextMenu_openRecordingsDir;
    }
}
