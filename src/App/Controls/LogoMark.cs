using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TcpDebug.App.Controls
{
    /// <summary>标题栏左侧的品牌标记：蓝底双向箭头。</summary>
    public sealed class LogoMark : Control
    {
        public LogoMark()
        {
            SetStyle(ControlStyles.UserPaint
                     | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.ResizeRedraw
                     | ControlStyles.SupportsTransparentBackColor, true);

            Size = new Size(26, 26);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Parent != null ? Parent.BackColor : Theme.Background);

            var rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            using (var path = Draw.RoundedRect(rect, Width * 0.28f))
            using (var brush = new LinearGradientBrush(rect, Theme.Accent, Theme.Mix(Theme.Accent, Color.Black, 0.4), 55f))
            {
                graphics.FillPath(brush, path);
            }

            var scale = Width / 26f;
            var penWidth = Math.Max(1.6f, 2f * scale);
            using (var pen = new Pen(Color.White, penWidth))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                graphics.DrawLine(pen, 7f * scale, 10f * scale, 17f * scale, 10f * scale);
                graphics.DrawLine(pen, 9f * scale, 17f * scale, 19f * scale, 17f * scale);
            }

            using (var brush = new SolidBrush(Color.White))
            {
                graphics.FillPolygon(brush, new[]
                {
                    new PointF(16.5f * scale, 7f * scale),
                    new PointF(21.5f * scale, 10f * scale),
                    new PointF(16.5f * scale, 13f * scale)
                });

                graphics.FillPolygon(brush, new[]
                {
                    new PointF(9.5f * scale, 14f * scale),
                    new PointF(4.5f * scale, 17f * scale),
                    new PointF(9.5f * scale, 20f * scale)
                });
            }
        }
    }
}
