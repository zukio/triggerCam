using System;
using System.Windows.Forms;

namespace triggerCam.Controls
{
    public class RecordingPathControl : UserControl
    {
        private TextBox textBox;
        private Button browseButton;
        
        public string Path
        {
            get => textBox.Text;
            set => textBox.Text = value;
        }
        
        public event EventHandler? PathChanged;
        public event EventHandler? BrowseClicked;

        public RecordingPathControl()
        {
            // テキストボックスを作成
            textBox = new TextBox
            {
                Width = 231,
                Dock = DockStyle.Left
            };
            textBox.TextChanged += (s, e) => PathChanged?.Invoke(this, e);
            
            // 参照ボタンを作成
            browseButton = new Button
            {
                Text = "...",
                Width = 40,
                Height = textBox.Height,
                Dock = DockStyle.Right
            };
            browseButton.Click += (s, e) => BrowseClicked?.Invoke(this, e);
            
            // コントロールの設定
            AutoSize = true;
            Controls.Add(browseButton);
            Controls.Add(textBox);
            Width = textBox.Width + browseButton.Width;
            Height = Math.Max(textBox.Height, browseButton.Height);
        }
    }
    
    public class RecordingPathToolStripItem : ToolStripControlHost
    {
        public RecordingPathControl PathControl => Control as RecordingPathControl ?? throw new InvalidOperationException();
        
        public string Path
        {
            get => PathControl.Path;
            set => PathControl.Path = value;
        }
        
        public event EventHandler? PathChanged
        {
            add => PathControl.PathChanged += value;
            remove => PathControl.PathChanged -= value;
        }
        
        public event EventHandler? BrowseClicked
        {
            add => PathControl.BrowseClicked += value;
            remove => PathControl.BrowseClicked -= value;
        }

        public RecordingPathToolStripItem() : base(new RecordingPathControl())
        {
            AutoSize = false;
            Width = 271; // テキストボックス + ボタンの幅
        }
    }
}
