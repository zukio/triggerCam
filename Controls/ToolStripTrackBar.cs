using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace micNotify.Controls
{
    /// <summary>
    /// ToolStripに配置できるTrackBarコントロール
    /// </summary>
    [ToolStripItemDesignerAvailability(ToolStripItemDesignerAvailability.ContextMenuStrip)]
    public class ToolStripTrackBar : ToolStripControlHost
    {
        private bool isDragging = false;
        private int pendingValue = 0;
        private EventHandler? valueChangedHandler = null;

        public ToolStripTrackBar() : base(new TrackBar())
        {
            TrackBar.TickStyle = TickStyle.Both;
            TrackBar.AutoSize = false;
            
            // マウスイベントを追加
            TrackBar.MouseDown += OnTrackBarMouseDown;
            TrackBar.MouseMove += OnTrackBarMouseMove;
            TrackBar.MouseUp += OnTrackBarMouseUp;
            
            // 標準のValueChangedイベントを無効化（マウスアップ時に自前で発火する）
            TrackBar.ValueChanged += (s, e) => 
            {
                if (!isDragging)
                {
                    // ドラッグ中でない場合（プログラムによる変更）のみイベントを発火
                    valueChangedHandler?.Invoke(s, e);
                }
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // マウスイベントを解除
                TrackBar.MouseDown -= OnTrackBarMouseDown;
                TrackBar.MouseMove -= OnTrackBarMouseMove;
                TrackBar.MouseUp -= OnTrackBarMouseUp;
            }
            base.Dispose(disposing);
        }

        public TrackBar TrackBar => Control as TrackBar;

        // TrackBarのプロパティをラップ
        public int Minimum
        {
            get { return TrackBar.Minimum; }
            set { TrackBar.Minimum = value; }
        }

        public int Maximum
        {
            get { return TrackBar.Maximum; }
            set { TrackBar.Maximum = value; }
        }

        public int Value
        {
            get { return TrackBar.Value; }
            set 
            { 
                TrackBar.Value = value;
                pendingValue = value;
            }
        }

        public int TickFrequency
        {
            get { return TrackBar.TickFrequency; }
            set { TrackBar.TickFrequency = value; }
        }

        // ValueChangedイベントをラップ
        public event EventHandler ValueChanged
        {
            add { valueChangedHandler += value; }
            remove { valueChangedHandler -= value; }
        }
        
        // マウスダウンイベント
        private void OnTrackBarMouseDown(object? sender, MouseEventArgs e)
        {
            isDragging = true;
            UpdateValueFromMousePosition(e.X);
        }
        
        // マウスムーブイベント
        private void OnTrackBarMouseMove(object? sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                UpdateValueFromMousePosition(e.X);
            }
        }
        
        // マウスアップイベント
        private void OnTrackBarMouseUp(object? sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                
                // 最終位置を設定
                UpdateValueFromMousePosition(e.X);
                
                // 値が変更された場合のみイベントを発火
                if (TrackBar.Value != pendingValue)
                {
                    pendingValue = TrackBar.Value;
                    valueChangedHandler?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        
        // マウス位置からスライダー値を更新
        private void UpdateValueFromMousePosition(int x)
        {
            if (TrackBar == null) return;
            
            // トラックバーの幅を取得
            int width = TrackBar.Width;
            if (width <= 0) return;
            
            // マウス位置を0〜1の範囲に正規化
            float normalized = Math.Max(0, Math.Min(1, (float)x / width));
            
            // 正規化された値をスライダーの範囲に変換
            int range = Maximum - Minimum;
            int newValue = Minimum + (int)(normalized * range);
            
            // 値を設定
            TrackBar.Value = Math.Max(Minimum, Math.Min(Maximum, newValue));
        }
    }
}
