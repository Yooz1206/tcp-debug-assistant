using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TcpDebug.App.Controls
{
    /// <summary>开关（拨动）控件。</summary>
    public sealed class ToggleSwitch : Control
    {
        private bool _checked;

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.UserPaint
                     | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.ResizeRedraw
                     | ControlStyles.SupportsTransparentBackColor, true);

            Size = new Size(36, 20);
            Cursor = Cursors.Hand;
            TabStop = false;
            BackColor = Color.Transparent;
        }

        public event EventHandler CheckedChanged;

        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked == value)
                {
                    return;
                }

                _checked = value;
                Invalidate();
                var handler = CheckedChanged;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Parent != null ? Parent.BackColor : Theme.Surface);

            var track = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            var trackColor = _checked ? Theme.Accent : Theme.Border;
            Draw.FillRounded(graphics, track, Height / 2f, trackColor);

            var knobSize = Height - 5f;
            var knobX = _checked ? Width - knobSize - 2.5f : 2.5f;
            var knob = new RectangleF(knobX, 2.5f, knobSize, knobSize);

            using (var brush = new SolidBrush(Color.White))
            {
                graphics.FillEllipse(brush, knob);
            }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            Checked = !Checked;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space)
            {
                Checked = !Checked;
            }
        }
    }
}
