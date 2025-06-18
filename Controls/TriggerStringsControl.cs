using System;
using System.Windows.Forms;

namespace triggerCam.Controls
{
    /// <summary>
    /// シリアルトリガー文字列を設定するためのコントロール
    /// </summary>
    public class TriggerStringsControl : UserControl
    {
        private TextBox snapTextBox;
        private TextBox startTextBox;
        private TextBox stopTextBox;

        public string SnapTrigger
        {
            get => snapTextBox.Text;
            set => snapTextBox.Text = value;
        }
        public string StartTrigger
        {
            get => startTextBox.Text;
            set => startTextBox.Text = value;
        }
        public string StopTrigger
        {
            get => stopTextBox.Text;
            set => stopTextBox.Text = value;
        }

        public event EventHandler? SettingsChanged;

        public TriggerStringsControl()
        {
            var panel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            panel.Controls.Add(new Label
            {
                Text = "SNAP:",
                AutoSize = true,
                TextAlign = System.Drawing.ContentAlignment.MiddleRight,
                Margin = new Padding(3, 3, 3, 3)
            });
            snapTextBox = new TextBox { Width = 60, Margin = new Padding(3, 3, 3, 3) };
            snapTextBox.TextChanged += (s, e) => SettingsChanged?.Invoke(this, e);
            panel.Controls.Add(snapTextBox);

            panel.Controls.Add(new Label
            {
                Text = "START:",
                AutoSize = true,
                TextAlign = System.Drawing.ContentAlignment.MiddleRight,
                Margin = new Padding(3, 3, 3, 3)
            });
            startTextBox = new TextBox { Width = 60, Margin = new Padding(3, 3, 3, 3) };
            startTextBox.TextChanged += (s, e) => SettingsChanged?.Invoke(this, e);
            panel.Controls.Add(startTextBox);

            panel.Controls.Add(new Label
            {
                Text = "STOP:",
                AutoSize = true,
                TextAlign = System.Drawing.ContentAlignment.MiddleRight,
                Margin = new Padding(3, 3, 3, 3)
            });
            stopTextBox = new TextBox { Width = 60, Margin = new Padding(3, 3, 3, 3) };
            stopTextBox.TextChanged += (s, e) => SettingsChanged?.Invoke(this, e);
            panel.Controls.Add(stopTextBox);

            Controls.Add(panel);
            AutoSize = true;
            MinimumSize = new System.Drawing.Size(250, 30);
        }
    }

    /// <summary>
    /// ToolStripに配置できるTriggerStringsControl
    /// </summary>
    public class TriggerStringsToolStripItem : ToolStripControlHost
    {
        public TriggerStringsControl TriggerStringsControl => Control as TriggerStringsControl ?? throw new InvalidOperationException();

        public string SnapTrigger
        {
            get => TriggerStringsControl.SnapTrigger;
            set => TriggerStringsControl.SnapTrigger = value;
        }
        public string StartTrigger
        {
            get => TriggerStringsControl.StartTrigger;
            set => TriggerStringsControl.StartTrigger = value;
        }
        public string StopTrigger
        {
            get => TriggerStringsControl.StopTrigger;
            set => TriggerStringsControl.StopTrigger = value;
        }

        public event EventHandler? SettingsChanged
        {
            add => TriggerStringsControl.SettingsChanged += value;
            remove => TriggerStringsControl.SettingsChanged -= value;
        }

        public TriggerStringsToolStripItem() : base(new TriggerStringsControl())
        {
            AutoSize = false;
            Width = 271;
            Height = 30;
        }
    }
}
