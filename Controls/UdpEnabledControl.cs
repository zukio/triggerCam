using System;
using System.Windows.Forms;

namespace triggerCam.Controls
{
    /// <summary>
    /// UDP有効設定用のチェックボックスコントロール
    /// </summary>
    public class UdpEnabledControl : UserControl
    {
        private CheckBox udpEnabledCheckBox;

        public bool UdpEnabled
        {
            get => udpEnabledCheckBox.Checked;
            set => udpEnabledCheckBox.Checked = value;
        }

        public event EventHandler? CheckedChanged;

        public UdpEnabledControl()
        {
            udpEnabledCheckBox = new CheckBox
            {
                Text = "UDP有効",
                AutoSize = true,
                Dock = DockStyle.Top,
                Checked = true
            };
            udpEnabledCheckBox.CheckedChanged += (s, e) => CheckedChanged?.Invoke(this, e);

            AutoSize = true;
            MinimumSize = new System.Drawing.Size(250, 30);

            Controls.Add(udpEnabledCheckBox);
        }
    }

    /// <summary>
    /// ToolStripに配置できるUDP有効コントロール
    /// </summary>
    public class UdpEnabledToolStripItem : ToolStripControlHost
    {
        public UdpEnabledControl UdpEnabledControl => Control as UdpEnabledControl ?? throw new InvalidOperationException();

        public bool UdpEnabled
        {
            get => UdpEnabledControl.UdpEnabled;
            set => UdpEnabledControl.UdpEnabled = value;
        }

        public event EventHandler? CheckedChanged
        {
            add => UdpEnabledControl.CheckedChanged += value;
            remove => UdpEnabledControl.CheckedChanged -= value;
        }

        public UdpEnabledToolStripItem() : base(new UdpEnabledControl())
        {
            AutoSize = false;
            Width = 271;
            Height = 30;
        }
    }
}
