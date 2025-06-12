﻿using System.Windows.Forms;
using micNotify.Controls;

namespace micNotify
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
            contextMenu_select = new ToolStripComboBox();
            contextMenu_state = new ToolStripMenuItem();
            contextMenu_micToggle = new ToolStripMenuItem();
            contextMenu_recToggle = new ToolStripMenuItem();
            contextMenu_threshold = new ThresholdToolStripItem();
            contextMenu_address = new ToolStripTextBox();
            contextMenu_recordingsDirLabel = new ToolStripMenuItem();
            contextMenu_recordingsPath = new RecordingPathToolStripItem();
            contextMenu_openRecordingsDir = new ToolStripMenuItem();
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
						// contextMenu_tempFileSettingsを初期化
						contextMenu_tempFileSettings = new TempFileSettingsToolStripItem();
						contextMenu_tempFileSettings.Name = "contextMenu_tempFileSettings";
						contextMenu_tempFileSettings.Size = new Size(271, 30);
						contextMenu_tempFileSettings.AutoSize = false;
            context.Items.AddRange(new ToolStripItem[] { 
							contextMenu_select, 
							contextMenu_state, 
							contextMenu_micToggle, 
							contextMenu_recToggle, 
							contextMenu_threshold, 
							contextMenu_tempFileSettings, 
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
            // contextMenu_select
            // 
            contextMenu_select.Items.AddRange(new object[] { "Data" });
            contextMenu_select.Name = "contextMenu_select";
            contextMenu_select.Size = new Size(271, 38);
            contextMenu_select.Text = "Select Microphne";
            // 
            // contextMenu_state
            // 
            contextMenu_state.Enabled = false;
            contextMenu_state.Margin = new Padding(30, 0, 0, 0);
            contextMenu_state.Name = "contextMenu_state";
            contextMenu_state.Size = new Size(331, 36);
            contextMenu_state.Text = "State:";
            // 
            // contextMenu_micToggle
            // 
            contextMenu_micToggle.Name = "contextMenu_micToggle";
            contextMenu_micToggle.Size = new Size(331, 36);
            contextMenu_micToggle.Text = "マイクON/OFF";
            contextMenu_micToggle.Click += contextMenu_micToggle_Click;
            // 
            // contextMenu_recToggle
            // 
            contextMenu_recToggle.Name = "contextMenu_recToggle";
            contextMenu_recToggle.Size = new Size(331, 36);
            contextMenu_recToggle.Text = "録音する";
            contextMenu_recToggle.Click += contextMenu_recToggle_Click;
            // 
            // contextMenu_threshold
            // 
            contextMenu_threshold.Name = "contextMenu_threshold";
            contextMenu_threshold.Size = new Size(300, 36);
            contextMenu_threshold.LabelText = "録音レベル";
            contextMenu_threshold.Minimum = 50;
            contextMenu_threshold.Maximum = 5000;
            contextMenu_threshold.Value = 500;
            contextMenu_threshold.TickFrequency = 500;
            contextMenu_threshold.ValueChanged += new EventHandler(contextMenu_threshold_ValueChanged);
            // 
            // contextMenu_recordingsDirLabel
            // 
            contextMenu_recordingsDirLabel.Name = "contextMenu_recordingsDirLabel";
            contextMenu_recordingsDirLabel.Size = new Size(331, 36);
            contextMenu_recordingsDirLabel.Text = "録音データの保存場所:";
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
            contextMenu_openRecordingsDir.Text = "録音データを開く";
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
            contextMenu_save.Text = "変更して再起動";
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
        private ToolStripComboBox contextMenu_select;
        private ToolStripMenuItem contextMenu_state;
        private ToolStripMenuItem contextMenu_micToggle;
        private ToolStripMenuItem contextMenu_recToggle;
        private ThresholdToolStripItem contextMenu_threshold;
        private ToolStripMenuItem contextMenu_save;
        private ToolStripMenuItem contextMenu_exit;
        private NotifyIcon notifyIcon1;
        private ToolStripTextBox contextMenu_address;
        private ToolStripMenuItem contextMenu_recordingsDirLabel;
        private RecordingPathToolStripItem contextMenu_recordingsPath;
        private ToolStripMenuItem contextMenu_openRecordingsDir;
				private TempFileSettingsToolStripItem contextMenu_tempFileSettings;
    }
}
