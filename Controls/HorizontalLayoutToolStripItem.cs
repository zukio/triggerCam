using System;
using System.Windows.Forms;

namespace triggerCam.Controls
{
    /// <summary>
    /// ContextMenuStrip内でラベルとコンボボックスを横並びに表示するためのカスタムコントロール
    /// </summary>
    public class HorizontalLayoutToolStripItem : ToolStripControlHost
    {
        private FlowLayoutPanel _flowPanel;
        private Label _label;
        private ComboBox _comboBox;

        /// <summary>
        /// ラベルテキストを取得または設定します
        /// </summary>
        public string LabelText
        {
            get { return _label.Text; }
            set { _label.Text = value; }
        }

        /// <summary>
        /// コンボボックスに関連付けられたイベントハンドラを取得します
        /// </summary>
        public event EventHandler? SelectedIndexChanged
        {
            add { _comboBox.SelectedIndexChanged += value; }
            remove { _comboBox.SelectedIndexChanged -= value; }
        }

        /// <summary>
        /// コンボボックスの項目コレクションを取得します
        /// </summary>
        public ComboBox.ObjectCollection Items
        {
            get { return _comboBox.Items; }
        }

        /// <summary>
        /// 指定されたインデックスの項目を取得します（nullセーフ）
        /// </summary>
        public object? GetItemAt(int index)
        {
            if (index >= 0 && index < _comboBox.Items.Count)
            {
                return _comboBox.Items[index];
            }
            return null;
        }

        /// <summary>
        /// コンボボックスの項目をクリアします
        /// </summary>
        public void ClearItems()
        {
            _comboBox.Items.Clear();
        }

        /// <summary>
        /// コンボボックスに項目を追加します
        /// </summary>
        public void AddItems(object[] items)
        {
            if (items != null)
            {
                _comboBox.Items.AddRange(items);
            }
        }

        /// <summary>
        /// コンボボックスの選択インデックスを取得または設定します
        /// </summary>
        public int SelectedIndex
        {
            get { return _comboBox.SelectedIndex; }
            set 
            {
                if (value >= 0 && value < _comboBox.Items.Count)
                {
                    _comboBox.SelectedIndex = value; 
                }
            }
        }

        /// <summary>
        /// コンボボックスのテキストを取得または設定します
        /// </summary>
        public string ComboBoxText
        {
            get { return _comboBox.Text; }
            set 
            { 
                _comboBox.Text = value;
                // テキストと一致する項目があれば選択
                for (int i = 0; i < _comboBox.Items.Count; i++)
                {
                    var item = _comboBox.Items[i];
                    if (item != null && item.ToString() == value)
                    {
                        _comboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 新しいインスタンスを初期化します
        /// </summary>
        /// <param name="labelText">ラベルのテキスト</param>
        /// <param name="comboBoxWidth">コンボボックスの幅（オプション）</param>
        public HorizontalLayoutToolStripItem(string labelText, int comboBoxWidth = 100) 
            : base(CreateCustomControl(labelText, comboBoxWidth))
        {
            // コントロールへの参照を保持
            _flowPanel = (FlowLayoutPanel)Control;
            _label = (Label)_flowPanel.Controls[0];
            _comboBox = (ComboBox)_flowPanel.Controls[1];
        }

        /// <summary>
        /// カスタムコントロールを作成します
        /// </summary>
        private static Control CreateCustomControl(string labelText, int comboBoxWidth)
        {
            // 水平方向のFlowLayoutPanelを作成
            var panel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            // ラベルを作成
            var label = new Label
            {
                Text = labelText,
                AutoSize = true,
                TextAlign = System.Drawing.ContentAlignment.MiddleRight,
                Margin = new Padding(3, 3, 3, 3)
            };

            // コンボボックスを作成
            var comboBox = new ComboBox
            {
                Width = comboBoxWidth,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(3, 3, 3, 3)
            };

            // コントロールをパネルに追加
            panel.Controls.Add(label);
            panel.Controls.Add(comboBox);

            return panel;
        }
    }
}
