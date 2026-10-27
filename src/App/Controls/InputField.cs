using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TcpDebug.App.Controls
{
    /// <summary>
    /// 带边框和焦点高亮的输入框。单行时文本垂直居中，多行时自适应。
    /// </summary>
    public sealed class InputField : Panel
    {
        private readonly Label _placeholder;
        private readonly bool _multiline;
        private bool _focused;

        public InputField() : this(false)
        {
        }

        public InputField(bool multiline)
        {
            _multiline = multiline;

            SetStyle(ControlStyles.UserPaint
                     | ControlStyles.AllPaintingInWmPaint
                     | ControlStyles.OptimizedDoubleBuffer
                     | ControlStyles.ResizeRedraw, true);

            BackColor = Theme.Input;
            Height = multiline ? 84 : 32;
            Padding = multiline ? new Padding(10, 8, 10, 8) : new Padding(10, 0, 10, 0);

            Box = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Theme.Input,
                ForeColor = Theme.Text,
                Font = multiline ? Theme.Mono : Theme.Ui,
                Multiline = multiline
            };

            if (multiline)
            {
                Box.Dock = DockStyle.Fill;
                Box.ScrollBars = ScrollBars.Vertical;
                Box.AcceptsReturn = true;
                Box.WordWrap = true;
            }

            Controls.Add(Box);

            _placeholder = new Label
            {
                AutoSize = true,
                BackColor = Theme.Input,
                ForeColor = Theme.TextFaint,
                Font = Box.Font,
                Visible = false,
                Cursor = Cursors.IBeam
            };
            _placeholder.Click += (sender, args) => Box.Focus();
            Controls.Add(_placeholder);

            Box.TextChanged += OnBoxTextChanged;
            Box.GotFocus += OnBoxGotFocus;
            Box.LostFocus += OnBoxLostFocus;
            Resize += (sender, args) => LayoutChildren();

            LayoutChildren();
        }

        public TextBox Box { get; private set; }

        public string PlaceholderText
        {
            get { return _placeholder.Text; }
            set
            {
                _placeholder.Text = value;
                UpdatePlaceholder();
            }
        }

        public string Value
        {
            get { return Box.Text; }
            set { Box.Text = value; }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Parent != null ? Parent.BackColor : Theme.Surface);

            var rectangle = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            using (var path = Draw.RoundedRect(rectangle, 6f))
            using (var brush = new SolidBrush(_focused ? Theme.Mix(Theme.Input, Color.White, 0.03) : Theme.Input))
            {
                graphics.FillPath(brush, path);
            }

            var borderColor = _focused ? Theme.Accent : Theme.Border;
            using (var path = Draw.RoundedRect(rectangle, 6f))
            using (var pen = new Pen(borderColor))
            {
                graphics.DrawPath(pen, path);
            }
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Box.Focus();
        }

        private void OnBoxGotFocus(object sender, EventArgs e)
        {
            _focused = true;
            Invalidate();
        }

        private void OnBoxLostFocus(object sender, EventArgs e)
        {
            _focused = false;
            Invalidate();
            UpdatePlaceholder();
        }

        private void OnBoxTextChanged(object sender, EventArgs e)
        {
            UpdatePlaceholder();
        }

        private void UpdatePlaceholder()
        {
            _placeholder.Visible = Box.TextLength == 0 && !string.IsNullOrEmpty(_placeholder.Text);
            if (!_multiline)
            {
                LayoutChildren();
            }
        }

        private void LayoutChildren()
        {
            if (_multiline)
            {
                _placeholder.Location = new Point(Padding.Left + 1, Padding.Top + 1);
                return;
            }

            var boxHeight = Box.PreferredHeight;
            var y = Math.Max(Padding.Top, (Height - boxHeight) / 2);
            Box.Bounds = new Rectangle(Padding.Left, y, Math.Max(10, Width - Padding.Horizontal), boxHeight);
            _placeholder.Location = new Point(Padding.Left + 1, y + 2);
        }
    }
}
