using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using TcpDebug.Core;

namespace TcpDebug.App.Controls
{
    public enum LogKind
    {
        Received,
        Sent,
        Info,
        Error,
        Notice
    }

    /// <summary>
    /// 收发日志视图：按方向着色、可暂停、自动裁剪，避免长时间运行后卡顿。
    /// </summary>
    public sealed class LogView : UserControl
    {
        private const int MaxChars = 400000;
        private const int TrimToChars = 300000;
        private const int MaxQueuedRecords = 4000;

        private readonly RichTextBox _box;
        private readonly Queue<LogRecord> _queue = new Queue<LogRecord>();
        private readonly System.Windows.Forms.Timer _flushTimer;
        private bool _showTimestamps = true;
        private bool _paused;

        public LogView()
        {
            BackColor = Theme.Input;
            Padding = new Padding(0);

            _box = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = Theme.Input,
                ForeColor = Theme.LogBody,
                Font = Theme.Mono,
                ReadOnly = true,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both,
                DetectUrls = false,
                HideSelection = false,
                ShortcutsEnabled = true
            };

            Controls.Add(_box);
            BuildContextMenu();

            _flushTimer = new System.Windows.Forms.Timer { Interval = 60 };
            _flushTimer.Tick += (sender, args) => Flush();
            _flushTimer.Start();
        }

        public event EventHandler SaveRequested;

        /// <summary>供外部设置深色滚动条等窗口特性。</summary>
        public RichTextBox Inner
        {
            get { return _box; }
        }

        public bool ShowTimestamps
        {
            get { return _showTimestamps; }
            set { _showTimestamps = value; }
        }

        public bool Paused
        {
            get { return _paused; }
            set
            {
                _paused = value;
                if (!value)
                {
                    Flush();
                }
            }
        }

        public string PlainText
        {
            get { return _box.Text; }
        }

        public void Append(LogKind kind, string text, int? byteCount = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action<LogKind, string, int?>(Append), kind, text, byteCount);
                }
                catch (InvalidOperationException)
                {
                }

                return;
            }

            _queue.Enqueue(new LogRecord { Time = DateTime.Now, Kind = kind, Text = text, ByteCount = byteCount });
            while (_queue.Count > MaxQueuedRecords)
            {
                _queue.Dequeue();
            }
        }

        public void Clear()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(Clear));
                return;
            }

            _queue.Clear();
            _box.Clear();
        }

        public void ScrollToEnd()
        {
            _box.SelectionStart = _box.TextLength;
            _box.SelectionLength = 0;
            _box.ScrollToCaret();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _flushTimer.Stop();
                _flushTimer.Dispose();
            }

            base.Dispose(disposing);
        }

        private void Flush()
        {
            if (_paused || _queue.Count == 0 || _box.IsDisposed)
            {
                return;
            }

            if (_queue.Count > 200)
            {
                _box.SuspendLayout();
            }

            while (_queue.Count > 0)
            {
                Render(_queue.Dequeue());
            }

            Trim();
            _box.ResumeLayout();

            if (_box.SelectionLength == 0)
            {
                ScrollToEnd();
            }
        }

        private void Render(LogRecord record)
        {
            _box.SelectionStart = _box.TextLength;
            _box.SelectionLength = 0;

            if (_showTimestamps)
            {
                _box.SelectionColor = Theme.LogTimestamp;
                _box.AppendText(record.Time.ToString("HH:mm:ss.fff") + "  ");
            }

            string arrow;
            Color color;
            switch (record.Kind)
            {
                case LogKind.Received:
                    arrow = "←";
                    color = Theme.LogReceived;
                    break;
                case LogKind.Sent:
                    arrow = "→";
                    color = Theme.LogSent;
                    break;
                case LogKind.Error:
                    arrow = "!";
                    color = Theme.LogError;
                    break;
                case LogKind.Notice:
                    arrow = "·";
                    color = Theme.LogNotice;
                    break;
                default:
                    arrow = "·";
                    color = Theme.LogInfo;
                    break;
            }

            _box.SelectionColor = color;
            _box.AppendText(arrow + " ");

            if (record.ByteCount.HasValue)
            {
                _box.SelectionColor = Theme.LogTimestamp;
                _box.AppendText("[" + ByteSize.Format(record.ByteCount.Value) + "] ");
            }

            _box.SelectionColor = record.Kind == LogKind.Received || record.Kind == LogKind.Sent
                ? Theme.LogBody
                : color;

            var indent = new string(' ', _showTimestamps ? 17 : 2);
            var body = record.Text.Replace("\r\n", "\n").Replace('\r', '\n');
            _box.AppendText(body.Replace("\n", "\n" + indent));
            _box.AppendText(Environment.NewLine);
        }

        private void Trim()
        {
            if (_box.TextLength <= MaxChars)
            {
                return;
            }

            var removeLength = _box.TextLength - TrimToChars;
            _box.ReadOnly = false;
            _box.Select(0, removeLength);
            _box.SelectedText = string.Empty;
            _box.ReadOnly = true;
            _box.SelectionStart = _box.TextLength;
        }

        private void BuildContextMenu()
        {
            var menu = new ContextMenuStrip
            {
                Renderer = new DarkMenuRenderer(),
                BackColor = Theme.Surface,
                ForeColor = Theme.Text,
                ShowImageMargin = false,
                Font = Theme.Ui
            };

            menu.Items.Add(CreateItem("复制", (sender, args) => _box.Copy()));
            menu.Items.Add(CreateItem("全选", (sender, args) => _box.SelectAll()));
            menu.Items.Add(new ToolStripSeparator());
            var pauseItem = CreateItem("暂停显示", null);
            pauseItem.Click += (sender, args) => Paused = !Paused;
            menu.Items.Add(pauseItem);
            menu.Items.Add(CreateItem("清空", (sender, args) => Clear()));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(CreateItem("保存日志…", (sender, args) =>
            {
                var handler = SaveRequested;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            }));

            _box.ContextMenuStrip = menu;
        }

        private static ToolStripMenuItem CreateItem(string text, EventHandler onClick)
        {
            var item = new ToolStripMenuItem(text) { ForeColor = Theme.TextMuted, Padding = new Padding(2, 3, 10, 3) };
            if (onClick != null)
            {
                item.Click += onClick;
            }

            return item;
        }

        private sealed class LogRecord
        {
            public DateTime Time;
            public LogKind Kind;
            public string Text;
            public int? ByteCount;
        }
    }
}
