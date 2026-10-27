using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace TcpDebug.App
{
    /// <summary>界面配色与字体。</summary>
    internal static class Theme
    {
        public static readonly Color Background = FromHex("#1B1D21");
        public static readonly Color Surface = FromHex("#22252A");
        public static readonly Color SurfaceRaised = FromHex("#2C3036");
        public static readonly Color Input = FromHex("#17191D");
        public static readonly Color Border = FromHex("#34383F");
        public static readonly Color BorderSoft = FromHex("#2A2E34");
        public static readonly Color Text = FromHex("#E6E8EB");
        public static readonly Color TextMuted = FromHex("#9AA1AB");
        public static readonly Color TextFaint = FromHex("#6B727C");

        public static readonly Color Accent = FromHex("#4D8DFF");
        public static readonly Color AccentHover = FromHex("#6BA1FF");
        public static readonly Color AccentPressed = FromHex("#3B78E7");
        public static readonly Color AccentSoft = FromHex("#243449");

        public static readonly Color Success = FromHex("#3FB950");
        public static readonly Color Warning = FromHex("#D9A03A");
        public static readonly Color Danger = FromHex("#F2555A");

        public static readonly Color LogReceived = FromHex("#7EE787");
        public static readonly Color LogSent = FromHex("#79C0FF");
        public static readonly Color LogInfo = FromHex("#8B929C");
        public static readonly Color LogError = FromHex("#FF7B72");
        public static readonly Color LogNotice = FromHex("#D2A8FF");
        public static readonly Color LogTimestamp = FromHex("#697079");
        public static readonly Color LogBody = FromHex("#CDD3DB");

        private static Font _ui;
        private static Font _uiSmall;
        private static Font _uiMedium;
        private static Font _title;
        private static Font _mono;
        private static Font _monoSmall;

        public static Font Ui
        {
            get { return _ui ?? (_ui = CreateUi(9f, FontStyle.Regular)); }
        }

        public static Font UiSmall
        {
            get { return _uiSmall ?? (_uiSmall = CreateUi(8.25f, FontStyle.Regular)); }
        }

        public static Font UiMedium
        {
            get { return _uiMedium ?? (_uiMedium = CreateUi(9.75f, FontStyle.Regular)); }
        }

        public static Font Title
        {
            get { return _title ?? (_title = CreateUi(10.5f, FontStyle.Bold)); }
        }

        public static Font Mono
        {
            get { return _mono ?? (_mono = CreateMono(9.75f)); }
        }

        public static Font MonoSmall
        {
            get { return _monoSmall ?? (_monoSmall = CreateMono(8.75f)); }
        }

        public static Color FromHex(string hex)
        {
            return ColorTranslator.FromHtml(hex);
        }

        public static Color Mix(Color from, Color to, double amount)
        {
            var clamped = Math.Max(0.0, Math.Min(1.0, amount));
            return Color.FromArgb(
                (int)(from.A + ((to.A - from.A) * clamped)),
                (int)(from.R + ((to.R - from.R) * clamped)),
                (int)(from.G + ((to.G - from.G) * clamped)),
                (int)(from.B + ((to.B - from.B) * clamped)));
        }

        private static Font CreateUi(float size, FontStyle style)
        {
            return new Font("Microsoft YaHei UI", size, style, GraphicsUnit.Point);
        }

        private static Font CreateMono(float size)
        {
            var candidates = new[] { "Cascadia Mono", "Consolas", "JetBrains Mono", "Courier New" };
            foreach (var name in candidates)
            {
                if (IsInstalled(name))
                {
                    return new Font(name, size, FontStyle.Regular, GraphicsUnit.Point);
                }
            }

            return new Font(FontFamily.GenericMonospace, size, FontStyle.Regular, GraphicsUnit.Point);
        }

        private static bool IsInstalled(string familyName)
        {
            try
            {
                using (var installed = new InstalledFontCollection())
                {
                    foreach (var family in installed.Families)
                    {
                        if (string.Equals(family.Name, familyName, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            return false;
        }
    }

    /// <summary>基础绘图辅助。</summary>
    internal static class Draw
    {
        public static GraphicsPath RoundedRect(RectangleF rect, float radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0.5f)
            {
                path.AddRectangle(rect);
                return path;
            }

            var diameter = radius * 2f;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180f, 90f);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270f, 90f);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0f, 90f);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90f, 90f);
            path.CloseFigure();
            return path;
        }

        public static void FillRounded(Graphics graphics, RectangleF rect, float radius, Color color)
        {
            using (var path = RoundedRect(rect, radius))
            using (var brush = new SolidBrush(color))
            {
                graphics.FillPath(brush, path);
            }
        }

        public static void StrokeRounded(Graphics graphics, RectangleF rect, float radius, Color color)
        {
            using (var path = RoundedRect(rect, radius))
            using (var pen = new Pen(color))
            {
                graphics.DrawPath(pen, path);
            }
        }

        public static void DrawText(Graphics graphics, string text, Font font, Rectangle bounds, Color color,
            TextFormatFlags flags)
        {
            TextRenderer.DrawText(graphics, text, font, bounds, color, flags);
        }

        public static int MeasureTextWidth(string text, Font font)
        {
            return TextRenderer.MeasureText(text, font, new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
        }
    }
}
