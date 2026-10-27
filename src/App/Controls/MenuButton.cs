using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace TcpDebug.App.Controls
{
    /// <summary>深色下拉菜单的配色。</summary>
    internal sealed class DarkColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground
        {
            get { return Theme.Surface; }
        }

        public override Color ImageMarginGradientBegin
        {
            get { return Theme.Surface; }
        }

        public override Color ImageMarginGradientMiddle
        {
            get { return Theme.Surface; }
        }

        public override Color ImageMarginGradientEnd
        {
            get { return Theme.Surface; }
        }

        public override Color MenuBorder
        {
            get { return Theme.Border; }
        }

        public override Color MenuItemBorder
        {
            get { return Theme.SurfaceRaised; }
        }

        public override Color MenuItemSelected
        {
            get { return Theme.SurfaceRaised; }
        }

        public override Color MenuItemSelectedGradientBegin
        {
            get { return Theme.SurfaceRaised; }
        }

        public override Color MenuItemSelectedGradientEnd
        {
            get { return Theme.SurfaceRaised; }
        }

        public override Color SeparatorDark
        {
            get { return Theme.Border; }
        }

        public override Color SeparatorLight
        {
            get { return Theme.Surface; }
        }
    }

    internal sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkColorTable())
        {
            RoundedEdges = false;
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Selected ? Theme.Text : Theme.Mix(Theme.Text, Theme.TextMuted, 0.35);
            base.OnRenderItemText(e);
        }
    }

    /// <summary>点击后弹出选项菜单的按钮。</summary>
    public sealed class MenuButton : FlatButton
    {
        private readonly List<string> _options = new List<string>();
        private string _prefix;
        private string _selectedText;
        private Action<string> _onSelected;

        public MenuButton()
        {
            Style = ButtonStyle.Secondary;
            AutoWidth = true;
            Font = Theme.UiSmall;
            Height = 28;
        }

        public string SelectedOption
        {
            get { return _selectedText; }
        }

        public void SetOptions(string prefix, IEnumerable<string> options, string selected, Action<string> onSelected)
        {
            _prefix = prefix;
            _options.Clear();
            _options.AddRange(options);
            _onSelected = onSelected;
            _selectedText = selected;
            RefreshText();
        }

        public void SelectOption(string option)
        {
            if (_selectedText == option)
            {
                return;
            }

            _selectedText = option;
            RefreshText();
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            ShowMenu();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var graphics = e.Graphics;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var centerY = Height / 2f;
            var x = Width - 14f;
            using (var pen = new Pen(Theme.TextMuted, 1.4f))
            {
                graphics.DrawLine(pen, x - 3.5f, centerY - 1.5f, x, centerY + 2f);
                graphics.DrawLine(pen, x, centerY + 2f, x + 3.5f, centerY - 1.5f);
            }
        }

        private void RefreshText()
        {
            Text = string.IsNullOrEmpty(_prefix)
                ? _selectedText + "    "
                : _prefix + " " + _selectedText + "    ";
        }

        private void ShowMenu()
        {
            var menu = new ContextMenuStrip
            {
                Renderer = new DarkMenuRenderer(),
                BackColor = Theme.Surface,
                ForeColor = Theme.Text,
                ShowImageMargin = false,
                Font = Theme.Ui,
                Padding = new Padding(0, 4, 0, 4)
            };

            foreach (var option in _options)
            {
                var isSelected = string.Equals(option, _selectedText, StringComparison.Ordinal);
                var item = new ToolStripMenuItem((isSelected ? "✓  " : "    ") + option)
                {
                    ForeColor = isSelected ? Theme.Text : Theme.TextMuted,
                    Padding = new Padding(2, 3, 10, 3)
                };

                var captured = option;
                item.Click += (sender, args) =>
                {
                    _selectedText = captured;
                    RefreshText();
                    if (_onSelected != null)
                    {
                        _onSelected(captured);
                    }
                };

                menu.Items.Add(item);
            }

            menu.Closed += (sender, args) => menu.Dispose();
            menu.Show(this, new Point(0, Height + 4));
        }
    }
}
