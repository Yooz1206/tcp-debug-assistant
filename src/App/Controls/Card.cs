using System;
using System.Drawing;
using System.Windows.Forms;

namespace TcpDebug.App.Controls
{
    /// <summary>
    /// 带标题栏和工具区的内容卡片。
    /// </summary>
    public sealed class Card : Panel
    {
        public Card(string title)
        {
            BackColor = Theme.Surface;
            Padding = new Padding(1);

            Header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = Theme.Surface,
                Padding = new Padding(14, 0, 8, 0)
            };

            TitleLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = title,
                ForeColor = Theme.Text,
                BackColor = Color.Transparent,
                Font = Theme.UiMedium,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false
            };

            Toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Theme.Surface,
                Padding = new Padding(0, 5, 0, 0),
                Margin = Padding.Empty
            };

            Body = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                Padding = new Padding(12)
            };

            // 先添加 Fill，再添加 Right，保证停靠顺序正确。
            Header.Controls.Add(TitleLabel);
            Header.Controls.Add(Toolbar);
            Controls.Add(Body);
            Controls.Add(Header);
        }

        public Panel Header { get; private set; }

        public Panel Body { get; private set; }

        public FlowLayoutPanel Toolbar { get; private set; }

        public Label TitleLabel { get; private set; }

        public string Title
        {
            get { return TitleLabel.Text; }
            set { TitleLabel.Text = value; }
        }

        /// <summary>在标题栏右侧的工具区添加控件。</summary>
        public void AddTool(Control control, int rightMargin = 6)
        {
            var margin = control.Margin;
            control.Margin = new Padding(margin.Left, margin.Top, rightMargin, margin.Bottom);
            Toolbar.Controls.Add(control);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var graphics = e.Graphics;
            using (var pen = new Pen(Theme.Border))
            {
                graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }

            using (var pen = new Pen(Theme.BorderSoft))
            {
                graphics.DrawLine(pen, 1, Header.Bottom - 1, Width - 2, Header.Bottom - 1);
            }
        }
    }
}
