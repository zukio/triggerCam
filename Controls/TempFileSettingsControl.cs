using System;
using System.Windows.Forms;

namespace triggerCam.Controls
{
    /// <summary>
    /// 一時ファイル設定用のカスタムコントロール
    /// </summary>
    public class TempFileSettingsControl : UserControl
    {
        private CheckBox useTempFileCheckBox;
        
        public bool UseTempFile
        {
            get => useTempFileCheckBox.Checked;
            set => useTempFileCheckBox.Checked = value;
        }
        
        public event EventHandler? SettingsChanged;

        public TempFileSettingsControl()
        {
            // チェックボックスを作成
            useTempFileCheckBox = new CheckBox
            {
                Text = "一時ファイルを使用する",
                AutoSize = true,
                Dock = DockStyle.Top,
                Checked = true // デフォルトでチェック
            };
            useTempFileCheckBox.CheckedChanged += (s, e) => SettingsChanged?.Invoke(this, e);
            
            // コントロールの設定
            AutoSize = true;
            MinimumSize = new Size(250, 30);
            
            // デバッグ用のログ
            Console.WriteLine("TempFileSettingsControl created");
            
            // コントロールを追加
            Controls.Add(useTempFileCheckBox);
        }
    }
    
    /// <summary>
    /// ToolStripに配置できる一時ファイル設定コントロール
    /// </summary>
    public class TempFileSettingsToolStripItem : ToolStripControlHost
    {
        public TempFileSettingsControl TempFileSettingsControl => Control as TempFileSettingsControl ?? throw new InvalidOperationException();
        
        public bool UseTempFile
        {
            get => TempFileSettingsControl.UseTempFile;
            set => TempFileSettingsControl.UseTempFile = value;
        }
        
        public event EventHandler? SettingsChanged
        {
            add => TempFileSettingsControl.SettingsChanged += value;
            remove => TempFileSettingsControl.SettingsChanged -= value;
        }

        public TempFileSettingsToolStripItem() : base(new TempFileSettingsControl())
        {
            AutoSize = false;
            Width = 271;
            Height = 30;
            
            // デバッグ用のログ
            Console.WriteLine("TempFileSettingsToolStripItem created");
        }
    }
}
