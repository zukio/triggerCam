using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;

namespace triggerCam.Controls
{
    /// <summary>
    /// ContextMenuStrip内で複数のコントロールを横並びに表示するためのカスタムコントロール
    /// </summary>
    public class HorizontalMultiControlToolStripItem : ToolStripControlHost
    {
        private FlowLayoutPanel _flowPanel;
        private List<Control> _controls = new List<Control>();

        /// <summary>
        /// 新しいインスタンスを初期化します
        /// </summary>
        public HorizontalMultiControlToolStripItem() 
            : base(CreateFlowPanel())
        {
            // コントロールへの参照を保持
            _flowPanel = (FlowLayoutPanel)Control;
        }

        /// <summary>
        /// FlowLayoutPanelを作成します
        /// </summary>
        private static Control CreateFlowPanel()
        {
            // 水平方向のFlowLayoutPanelを作成
            var panel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0),
                BackColor = Color.Transparent,
                MinimumSize = new Size(200, 25) // 最小サイズを設定してコンテキストメニューに表示されるようにする
            };

            return panel;
        }

        /// <summary>
        /// ComboBoxを追加します
        /// </summary>
        /// <param name="width">コンボボックスの幅</param>
        /// <returns>追加されたComboBox</returns>
        public ComboBox AddComboBox(int width = 100)
        {
            var comboBox = new ComboBox
            {
                Width = width,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(3, 3, 3, 3)
            };

            _flowPanel.Controls.Add(comboBox);
            _controls.Add(comboBox);
            return comboBox;
        }

        /// <summary>
        /// ラベルを追加します
        /// </summary>
        /// <param name="text">ラベルのテキスト</param>
        /// <returns>追加されたLabel</returns>
        public Label AddLabel(string text)
        {
            var label = new Label
            {
                Text = text,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleRight,
                Margin = new Padding(3, 3, 3, 3)
            };

            _flowPanel.Controls.Add(label);
            _controls.Add(label);
            return label;
        }

        /// <summary>
        /// ボタンを追加します
        /// </summary>
        /// <param name="text">ボタンのテキスト</param>
        /// <param name="backColor">ボタンの背景色</param>
        /// <returns>追加されたButton</returns>
        public Button AddButton(string text, Color backColor)
        {
            var button = new Button
            {
                Text = text,
                BackColor = backColor,
                AutoSize = true,
                Margin = new Padding(3, 3, 3, 3),
                FlatStyle = FlatStyle.Flat
            };

            _flowPanel.Controls.Add(button);
            _controls.Add(button);
            return button;
        }

        /// <summary>
        /// 指定したインデックスのコントロールを取得します
        /// </summary>
        /// <param name="index">インデックス</param>
        /// <returns>コントロール</returns>
        public Control? GetControlAt(int index)
        {
            if (index >= 0 && index < _controls.Count)
            {
                return _controls[index];
            }
            return null;
        }

        /// <summary>
        /// 指定したインデックスのコントロールを表示/非表示にします
        /// </summary>
        /// <param name="index">インデックス</param>
        /// <param name="visible">表示するかどうか</param>
        public void SetControlVisible(int index, bool visible)
        {
            if (index >= 0 && index < _controls.Count)
            {
                _controls[index].Visible = visible;
            }
        }

        /// <summary>
        /// 指定したコントロールを表示/非表示にします
        /// </summary>
        /// <param name="control">コントロール</param>
        /// <param name="visible">表示するかどうか</param>
        public void SetControlVisible(Control control, bool visible)
        {
            if (control != null && _controls.Contains(control))
            {
                control.Visible = visible;
            }
        }
    }
}
