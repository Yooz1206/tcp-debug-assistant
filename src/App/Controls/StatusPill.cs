using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TcpDebug.App.Controls
{
    /// <summary>带状态圆点的胶囊标签。</summary>
    public sealed class StatusPill : Control
    {
        private string _statusText = "未连接";
        private Color _dotColor = Theme.TextFaint;

        public StatusPill()
        {
            SetStyle(ControlStyles.UserPaint
                     | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.ResizeRedraw
                     | ControlStyles.SupportsTransparentBackColor, true);

            Height = 26;
            Font = Theme.UiSmall;
            BackColor = Color.Transparent;
            Width = 130;
        }

        public void SetStatus(string text, Color dotColor)
        {
            _statusText = text;
            _dotColor = dotColor;
            UpdateWidth();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Parent != null ? Parent.BackColor : Theme.Background);

            var rectangle = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            Draw.FillRounded(graphics, rectangle, Height / 2f, Theme.Surface);
            Draw.StrokeRounded(graphics, rectangle, Height / 2f, Theme.Border);

            using (var brush = new SolidBrush(_dotColor))
            {
                graphics.FillEllipse(brush, 10f, (Height / 2f) - 3f, 6f, 6f);
            }

            TextRenderer.DrawText(graphics, _statusText, Font,
                new Rectangle(22, 0, Width - 26, Height), Theme.Text,
                TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        }

        private void UpdateWidth()
        {
            Width = Draw.MeasureTextWidth(_statusText, Font) + 34;
        }
    }
}
