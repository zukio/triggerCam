using System;
using System.Drawing;
using System.Windows.Forms;

namespace micNotify.Controls
{
    public class ThresholdControl : UserControl
    {
        private Label label;
        private TrackBar trackBar;
        
        public int Value
        {
            get => trackBar.Value;
            set => trackBar.Value = value;
        }
        
        public int Minimum
        {
            get => trackBar.Minimum;
            set => trackBar.Minimum = value;
        }
        
        public int Maximum
        {
            get => trackBar.Maximum;
            set => trackBar.Maximum = value;
        }
        
        public int TickFrequency
        {
            get => trackBar.TickFrequency;
            set => trackBar.TickFrequency = value;
        }
        
        public string LabelText
        {
            get => label.Text;
            set => label.Text = value;
        }
        
        public event EventHandler? ValueChanged;

        public ThresholdControl()
        {
            // ラベルを作成
            label = new Label
            {
                Text = "録音レベル",
                Width = 100,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Left
            };
            
            // トラックバーを作成
            trackBar = new TrackBar
            {
                Width = 170,
                Minimum = 50,
                Maximum = 5000,
                Value = 500,
                TickFrequency = 500,
                TickStyle = TickStyle.None,
                Dock = DockStyle.Fill
            };
            trackBar.ValueChanged += (s, e) => ValueChanged?.Invoke(this, e);
            
            // コントロールの設定
            AutoSize = true;
            Controls.Add(trackBar);
            Controls.Add(label);
            Height = Math.Max(label.Height, trackBar.Height);
        }
    }
    
    public class ThresholdToolStripItem : ToolStripControlHost
    {
        public ThresholdControl ThresholdControl => Control as ThresholdControl ?? throw new InvalidOperationException();
        
        public int Value
        {
            get => ThresholdControl.Value;
            set => ThresholdControl.Value = value;
        }
        
        public int Minimum
        {
            get => ThresholdControl.Minimum;
            set => ThresholdControl.Minimum = value;
        }
        
        public int Maximum
        {
            get => ThresholdControl.Maximum;
            set => ThresholdControl.Maximum = value;
        }
        
        public int TickFrequency
        {
            get => ThresholdControl.TickFrequency;
            set => ThresholdControl.TickFrequency = value;
        }
        
        public string LabelText
        {
            get => ThresholdControl.LabelText;
            set => ThresholdControl.LabelText = value;
        }
        
        public event EventHandler? ValueChanged
        {
            add => ThresholdControl.ValueChanged += value;
            remove => ThresholdControl.ValueChanged -= value;
        }

        public ThresholdToolStripItem() : base(new ThresholdControl())
        {
            AutoSize = false;
            Width = 271; // ラベル + トラックバーの幅
        }
    }
}
