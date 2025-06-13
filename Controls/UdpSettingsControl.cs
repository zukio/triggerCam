using System;
using System.Windows.Forms;

namespace triggerCam.Controls
{
    /// <summary>
    /// UDP有効設定と送信先アドレスをまとめたコントロール
    /// </summary>
    public class UdpSettingsControl : UserControl
    {
        private CheckBox udpEnabledCheckBox;
        private TextBox addressTextBox;

        public bool UdpEnabled
        {
            get => udpEnabledCheckBox.Checked;
            set
            {
                udpEnabledCheckBox.Checked = value;
                addressTextBox.Visible = value;
            }
        }

        public string Address
        {
            get => addressTextBox.Text;
            set => addressTextBox.Text = value;
        }

        public event EventHandler? UdpEnabledChanged;
        public event EventHandler? AddressChanged;

        public UdpSettingsControl()
        {
            var panel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            udpEnabledCheckBox = new CheckBox
            {
                Text = "UDP有効",
                AutoSize = true,
                Checked = true,
                Margin = new Padding(3,3,3,3)
            };
            udpEnabledCheckBox.CheckedChanged += (s, e) =>
            {
                addressTextBox.Visible = udpEnabledCheckBox.Checked;
                UdpEnabledChanged?.Invoke(this, e);
            };

            addressTextBox = new TextBox
            {
                Width = 150,
                Margin = new Padding(3,3,3,3)
            };
            addressTextBox.TextChanged += (s, e) => AddressChanged?.Invoke(this, e);

            panel.Controls.Add(udpEnabledCheckBox);
            panel.Controls.Add(addressTextBox);

            Controls.Add(panel);
            AutoSize = true;
            MinimumSize = new System.Drawing.Size(250, 30);
        }
    }

    /// <summary>
    /// ToolStripに配置できるUDP設定コントロール
    /// </summary>
    public class UdpSettingsToolStripItem : ToolStripControlHost
    {
        public UdpSettingsControl UdpSettingsControl => Control as UdpSettingsControl ?? throw new InvalidOperationException();

        public bool UdpEnabled
        {
            get => UdpSettingsControl.UdpEnabled;
            set => UdpSettingsControl.UdpEnabled = value;
        }

        public string Address
        {
            get => UdpSettingsControl.Address;
            set => UdpSettingsControl.Address = value;
        }

        public event EventHandler? UdpEnabledChanged
        {
            add => UdpSettingsControl.UdpEnabledChanged += value;
            remove => UdpSettingsControl.UdpEnabledChanged -= value;
        }

        public event EventHandler? AddressChanged
        {
            add => UdpSettingsControl.AddressChanged += value;
            remove => UdpSettingsControl.AddressChanged -= value;
        }

        public UdpSettingsToolStripItem() : base(new UdpSettingsControl())
        {
            AutoSize = false;
            Width = 271;
            Height = 30;
        }
    }
}
