using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TcpDebug.App.Controls
{
    /// <summary>扁平风格按钮，支持主要 / 次要 / 幽灵 / 危险四种外观。</summary>
    public class FlatButton : Control
    {
        public enum ButtonStyle
        {
            Primary,
            Secondary,
            Ghost,
            Danger
        }

        private ButtonStyle _style = ButtonStyle.Secondary;
        private bool _hover;
        private bool _pressed;
        private bool _autoWidth;

        public FlatButton()
        {
            SetStyle(ControlStyles.UserPaint
                     | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.ResizeRedraw
                     | ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            ForeColor = Theme.Text;
            Font = Theme.Ui;
            Size = new Size(84, 30);
            Cursor = Cursors.Hand;
            TabStop = false;
            Radius = 6;
        }

        public ButtonStyle Style
        {
            get { return _style; }
            set
            {
                _style = value;
                Invalidate();
            }
        }

        public int Radius { get; set; }

        /// <summary>文本变化时自动调整宽度。</summary>
        public bool AutoWidth
        {
            get { return _autoWidth; }
            set
            {
                _autoWidth = value;
                if (value)
                {
                    UpdateWidth();
                }
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            if (_autoWidth)
            {
                UpdateWidth();
            }
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Cursor = Enabled ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hover = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hover = false;
            _pressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            _pressed = true;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _pressed = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Parent != null ? Parent.BackColor : Theme.Surface);

            var rectangle = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);

            Color fill;
            Color border;
            Color text;
            ResolveColors(out fill, out border, out text);

            if (fill.A > 0)
            {
                Draw.FillRounded(graphics, rectangle, Radius, fill);
            }

            if (border.A > 0)
            {
                Draw.StrokeRounded(graphics, rectangle, Radius, border);
            }

            var flags = TextFormatFlags.HorizontalCenter
                        | TextFormatFlags.VerticalCenter
                        | TextFormatFlags.NoPadding
                        | TextFormatFlags.SingleLine
                        | TextFormatFlags.EndEllipsis;

            TextRenderer.DrawText(graphics, Text, Font, Rectangle.Round(rectangle), text, flags);
        }

        private void ResolveColors(out Color fill, out Color border, out Color text)
        {
            if (!Enabled)
            {
                fill = _style == ButtonStyle.Primary ? Theme.Mix(Theme.Accent, Theme.Surface, 0.65) : Color.Transparent;
                border = _style == ButtonStyle.Secondary ? Theme.BorderSoft : Color.Transparent;
                text = Theme.TextFaint;
                return;
            }

            switch (_style)
            {
                case ButtonStyle.Primary:
                    fill = _pressed ? Theme.AccentPressed : (_hover ? Theme.AccentHover : Theme.Accent);
                    border = Color.Transparent;
                    text = Color.White;
                    break;

                case ButtonStyle.Ghost:
                    fill = _pressed ? Theme.SurfaceRaised : (_hover ? Theme.SurfaceRaised : Color.Transparent);
                    border = Color.Transparent;
                    text = _hover ? Theme.Text : Theme.TextMuted;
                    break;

                case ButtonStyle.Danger:
                    fill = _pressed ? Theme.Mix(Theme.Danger, Color.Black, 0.25) : (_hover ? Theme.Mix(Theme.Danger, Color.Black, 0.12) : Color.Transparent);
                    border = _hover ? Theme.Mix(Theme.Danger, Theme.Surface, 0.35) : Theme.BorderSoft;
                    text = Theme.Danger;
                    break;

                default:
                    fill = _pressed ? Theme.Mix(Theme.SurfaceRaised, Color.Black, 0.2) : (_hover ? Theme.SurfaceRaised : Theme.Mix(Theme.Surface, Theme.SurfaceRaised, 0.35));
                    border = _hover ? Theme.Border : Theme.Mix(Theme.Border, Theme.Surface, 0.35);
                    text = Theme.Text;
                    break;
            }
        }

        private void UpdateWidth()
        {
            var textWidth = Draw.MeasureTextWidth(Text, Font);
            Width = textWidth + Padding.Horizontal + 22;
        }
    }
}
