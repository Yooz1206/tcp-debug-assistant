using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TcpDebug.App.Controls
{
    /// <summary>分段选择控件（例如 客户端 / 服务端）。</summary>
    public sealed class SegmentedControl : Control
    {
        private readonly List<string> _items = new List<string>();
        private int _selectedIndex;
        private int _hoverIndex = -1;

        public SegmentedControl()
        {
            SetStyle(ControlStyles.UserPaint
                     | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.ResizeRedraw, true);

            BackColor = Theme.Input;
            ForeColor = Theme.Text;
            Font = Theme.Ui;
            Size = new Size(150, 30);
            Cursor = Cursors.Hand;
            TabStop = false;
        }

        public event EventHandler SelectionChanged;

        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                var clamped = Math.Max(0, Math.Min(_items.Count - 1, value));
                if (clamped == _selectedIndex)
                {
                    return;
                }

                _selectedIndex = clamped;
                Invalidate();
                var handler = SelectionChanged;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            }
        }

        public void SetItems(params string[] items)
        {
            _items.Clear();
            _items.AddRange(items);
            _selectedIndex = 0;
            Invalidate();
        }

        public string SelectedText
        {
            get
            {
                if (_selectedIndex < 0 || _selectedIndex >= _items.Count)
                {
                    return string.Empty;
                }

                return _items[_selectedIndex];
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Parent != null ? Parent.BackColor : Theme.Background);

            var outer = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            Draw.FillRounded(graphics, outer, Height / 2f, Theme.Input);
            Draw.StrokeRounded(graphics, outer, Height / 2f, Theme.Border);

            if (_items.Count == 0)
            {
                return;
            }

            var inner = new RectangleF(3f, 3f, Width - 6f, Height - 6f);
            var segmentWidth = inner.Width / _items.Count;

            for (var i = 0; i < _items.Count; i++)
            {
                var segment = new RectangleF(inner.X + (segmentWidth * i), inner.Y, segmentWidth, inner.Height);
                var isSelected = i == _selectedIndex;

                if (isSelected)
                {
                    Draw.FillRounded(graphics, segment, segment.Height / 2f, Theme.Accent);
                }
                else if (i == _hoverIndex)
                {
                    Draw.FillRounded(graphics, segment, segment.Height / 2f, Theme.SurfaceRaised);
                }

                var color = isSelected ? Color.White : Theme.TextMuted;
                TextRenderer.DrawText(graphics, _items[i], Font, Rectangle.Round(segment), color,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var index = HitTest(e.X);
            if (index != _hoverIndex)
            {
                _hoverIndex = index;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverIndex = -1;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            var index = HitTest(e.X);
            if (index >= 0)
            {
                SelectedIndex = index;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Left && _selectedIndex > 0)
            {
                SelectedIndex--;
            }
            else if (e.KeyCode == Keys.Right && _selectedIndex < _items.Count - 1)
            {
                SelectedIndex++;
            }
        }

        private int HitTest(int x)
        {
            if (_items.Count == 0)
            {
                return -1;
            }

            var innerWidth = Width - 6f;
            if (innerWidth <= 0)
            {
                return -1;
            }

            var index = (int)((x - 3f) / (innerWidth / _items.Count));
            return Math.Max(0, Math.Min(_items.Count - 1, index));
        }
    }
}
