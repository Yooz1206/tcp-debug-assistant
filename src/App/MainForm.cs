using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using TcpDebug.App.Controls;

namespace TcpDebug.App
{
    public sealed partial class MainForm : Form
    {
        private readonly AppOptions _options;
        private readonly TcpDebug.Core.Net.TcpClientLink _client = new TcpDebug.Core.Net.TcpClientLink();
        private readonly TcpDebug.Core.Net.TcpServerLink _server = new TcpDebug.Core.Net.TcpServerLink();
        private readonly TcpDebug.Core.TransferStats _stats = new TcpDebug.Core.TransferStats();
        private readonly TcpDebug.Core.SendHistory _history = new TcpDebug.Core.SendHistory(100);
        private readonly Dictionary<string, TcpDebug.Core.Net.TcpSession> _sessions =
            new Dictionary<string, TcpDebug.Core.Net.TcpSession>();
        private readonly List<string> _sessionOrder = new List<string>();

        private TableLayoutPanel _sideLayout;
        private SplitContainer _mainSplit;
        private SplitContainer _contentSplit;
        private Card _logCard;
        private Card _infoCard;
        private Card _clientsCard;
        private Card _tipsCard;
        private Card _sendCard;
        private SegmentedControl _modeSwitch;
        private SegmentedControl _viewSwitch;
        private InputField _hostField;
        private InputField _portField;
        private InputField _sendInput;
        private InputField _intervalField;
        private FlatButton _connectButton;
        private FlatButton _disconnectButton;
        private FlatButton _clearLogButton;
        private FlatButton _saveLogButton;
        private FlatButton _sendButton;
        private FlatButton _clearSendButton;
        private ToggleSwitch _timestampsToggle;
        private ToggleSwitch _pauseToggle;
        private ToggleSwitch _hexSendToggle;
        private ToggleSwitch _crlfToggle;
        private ToggleSwitch _autoSendToggle;
        private MenuButton _encodingMenu;
        private MenuButton _targetMenu;
        private StatusPill _statusPill;
        private LogView _log;
        private ListBox _clientList;
        private Label _clientEmptyLabel;
        private Label _stateValue;
        private Label _localValue;
        private Label _remoteValue;
        private Label _durationValue;
        private Label _countersLabel;
        private Label _payloadLabel;
        private System.Windows.Forms.Timer _uiTimer;
        private System.Windows.Forms.Timer _autoSendTimer;

        private bool _isServerMode;
        private bool _busy;
        private bool _autoSendUpdating;
        private DateTime _connectedAt;

        private bool HasConnection
        {
            get { return _client.IsConnected || _server.IsListening; }
        }

        public MainForm(AppOptions options)
        {
            _options = options ?? new AppOptions();

            BuildUi();
            CreateTimers();
            WireEvents();
            ApplyMode();

            var settings = AppSettings.Load();
            ApplySettings(settings);
            ApplyOptions();
            UpdateCounters();
        }

        private void CreateTimers()
        {
            _uiTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _uiTimer.Tick += (sender, args) => UpdateCounters();
            _uiTimer.Start();

            _autoSendTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _autoSendTimer.Tick += (sender, args) => SendCurrent(true);
        }

        private void BuildUi()
        {
            SuspendLayout();

            Text = "TCP 调试助手";
            BackColor = Theme.Background;
            Font = Theme.Ui;

            var workArea = Screen.PrimaryScreen.WorkingArea;
            var desiredWidth = Math.Min(1200, Math.Max(900, workArea.Width - 80));
            var desiredHeight = Math.Min(800, Math.Max(620, workArea.Height - 80));

            ClientSize = new Size(desiredWidth, desiredHeight);
            MinimumSize = new Size(Math.Min(1000, desiredWidth), Math.Min(680, desiredHeight));
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;
            KeyPreview = true;

            try
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch (Exception)
            {
            }

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Theme.Background,
                Padding = new Padding(14, 12, 14, 6),
                Margin = Padding.Empty
            };

            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            root.Controls.Add(BuildHeader(), 0, 0);
            root.Controls.Add(BuildMainArea(), 0, 1);
            root.Controls.Add(BuildStatusBar(), 0, 2);
            Controls.Add(root);

            ResumeLayout();
        }

        private Control BuildHeader()
        {
            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 10,
                RowCount = 1,
                BackColor = Theme.Background,
                Margin = Padding.Empty
            };

            header.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 172f));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 158f));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 168f));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82f));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112f));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var brand = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Theme.Background,
                Margin = Padding.Empty,
                Padding = new Padding(0, 9, 0, 0)
            };

            var logo = new LogoMark { Margin = new Padding(0, 0, 8, 0) };
            var title = new Label
            {
                Text = "TCP 调试助手",
                AutoSize = true,
                ForeColor = Theme.Text,
                Font = Theme.Title,
                Margin = new Padding(0, 4, 0, 0)
            };

            brand.Controls.Add(logo);
            brand.Controls.Add(title);
            header.Controls.Add(brand, 0, 0);

            _modeSwitch = new SegmentedControl
            {
                Size = new Size(150, 30),
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 8, 0)
            };
            _modeSwitch.SetItems("客户端", "服务端");
            header.Controls.Add(_modeSwitch, 1, 0);

            header.Controls.Add(CreateFieldLabel("地址"), 2, 0);

            _hostField = new InputField
            {
                Size = new Size(168, 32),
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 10, 0)
            };
            _hostField.Value = "127.0.0.1";
            _hostField.PlaceholderText = "目标地址";
            header.Controls.Add(_hostField, 3, 0);

            header.Controls.Add(CreateFieldLabel("端口"), 4, 0);

            _portField = new InputField
            {
                Size = new Size(82, 32),
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 0, 10, 0)
            };
            _portField.Value = "8080";
            _portField.PlaceholderText = "端口";
            header.Controls.Add(_portField, 5, 0);

            _connectButton = new FlatButton
            {
                Text = "连接",
                Style = FlatButton.ButtonStyle.Primary,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 6, 8, 6)
            };
            header.Controls.Add(_connectButton, 6, 0);

            _disconnectButton = new FlatButton
            {
                Text = "断开",
                Style = FlatButton.ButtonStyle.Secondary,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 6, 0, 6),
                Enabled = false
            };
            header.Controls.Add(_disconnectButton, 7, 0);

            _statusPill = new StatusPill
            {
                Anchor = AnchorStyles.Right,
                Margin = new Padding(12, 0, 0, 0)
            };
            _statusPill.SetStatus("未连接", Theme.TextFaint);
            header.Controls.Add(_statusPill, 9, 0);

            return header;
        }

        private Control BuildMainArea()
        {
            _contentSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 10,
                BackColor = Theme.Background,
                BorderStyle = BorderStyle.None,
                Margin = Padding.Empty
            };
            _contentSplit.Panel1.BackColor = Theme.Background;
            _contentSplit.Panel2.BackColor = Theme.Background;

            var logCard = BuildLogCard();
            logCard.Dock = DockStyle.Fill;
            _contentSplit.Panel1.Controls.Add(logCard);

            var side = BuildSidePanel();
            side.Dock = DockStyle.Fill;
            _contentSplit.Panel2.Controls.Add(side);

            _mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterWidth = 10,
                BackColor = Theme.Background,
                BorderStyle = BorderStyle.None,
                Margin = new Padding(0, 12, 0, 10)
            };
            _mainSplit.Panel1.BackColor = Theme.Background;
            _mainSplit.Panel2.BackColor = Theme.Background;

            _mainSplit.Panel1.Controls.Add(_contentSplit);

            var sendCard = BuildSendCard();
            sendCard.Dock = DockStyle.Fill;
            _mainSplit.Panel2.Controls.Add(sendCard);

            return _mainSplit;
        }

        private Card BuildLogCard()
        {
            _logCard = new Card("接收");
            _logCard.Body.Padding = new Padding(8);

            _log = new LogView { Dock = DockStyle.Fill };
            _logCard.Body.Controls.Add(_log);

            _viewSwitch = new SegmentedControl
            {
                Size = new Size(102, 26),
                Margin = new Padding(0, 5, 6, 0)
            };
            _viewSwitch.SetItems("文本", "HEX");

            _timestampsToggle = new ToggleSwitch { Margin = new Padding(0, 8, 4, 0) };
            _pauseToggle = new ToggleSwitch { Margin = new Padding(0, 8, 4, 0) };

            _clearLogButton = new FlatButton
            {
                Text = "清空",
                Style = FlatButton.ButtonStyle.Ghost,
                AutoWidth = true,
                Height = 26,
                Font = Theme.UiSmall,
                Margin = new Padding(0, 5, 4, 0)
            };

            _saveLogButton = new FlatButton
            {
                Text = "保存",
                Style = FlatButton.ButtonStyle.Ghost,
                AutoWidth = true,
                Height = 26,
                Font = Theme.UiSmall,
                Margin = new Padding(0, 5, 0, 0)
            };

            _logCard.AddTool(_viewSwitch, 8);
            _logCard.AddTool(CreateToolLabel("时间戳"), 4);
            _logCard.AddTool(_timestampsToggle, 14);
            _logCard.AddTool(CreateToolLabel("暂停"), 4);
            _logCard.AddTool(_pauseToggle, 16);
            _logCard.AddTool(_clearLogButton, 4);
            _logCard.AddTool(_saveLogButton, 4);

            return _logCard;
        }

        private Control BuildSidePanel()
        {
            _sideLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Theme.Background,
                Margin = Padding.Empty
            };
            _sideLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 202f));
            _sideLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            _sideLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            _infoCard = new Card("连接信息") { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 10) };
            _infoCard.Body.Padding = new Padding(12, 8, 12, 8);

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                BackColor = Theme.Surface,
                Margin = Padding.Empty
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54f));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            for (var i = 0; i < 4; i++)
            {
                grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));
            }

            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            grid.Controls.Add(CreateInfoKey("状态"), 0, 0);
            _stateValue = CreateInfoValue("未连接");
            grid.Controls.Add(_stateValue, 1, 0);

            grid.Controls.Add(CreateInfoKey("本地"), 0, 1);
            _localValue = CreateInfoValue("—");
            grid.Controls.Add(_localValue, 1, 1);

            grid.Controls.Add(CreateInfoKey("远端"), 0, 2);
            _remoteValue = CreateInfoValue("—");
            grid.Controls.Add(_remoteValue, 1, 2);

            grid.Controls.Add(CreateInfoKey("时长"), 0, 3);
            _durationValue = CreateInfoValue("—");
            grid.Controls.Add(_durationValue, 1, 3);

            _infoCard.Body.Controls.Add(grid);
            _sideLayout.Controls.Add(_infoCard, 0, 0);

            _clientsCard = new Card("客户端 (0)") { Dock = DockStyle.Fill, Margin = Padding.Empty, Visible = false };
            _clientsCard.Body.Padding = new Padding(8);

            _clientList = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Input,
                ForeColor = Theme.Text,
                BorderStyle = BorderStyle.None,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 26,
                IntegralHeight = false
            };
            _clientList.DrawItem += OnClientListDrawItem;

            _clientEmptyLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "暂无客户端",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Theme.TextFaint,
                BackColor = Theme.Input,
                Font = Theme.UiSmall
            };

            _clientsCard.Body.Controls.Add(_clientList);
            _clientsCard.Body.Controls.Add(_clientEmptyLabel);
            _clientEmptyLabel.BringToFront();
            _sideLayout.Controls.Add(_clientsCard, 0, 1);

            _tipsCard = new Card("使用提示") { Dock = DockStyle.Fill, Margin = Padding.Empty };
            _tipsCard.Body.Padding = new Padding(14, 10, 14, 10);

            var tips = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                ForeColor = Theme.TextMuted,
                Font = Theme.UiSmall,
                Text = "快捷键" + Environment.NewLine +
                       "Ctrl+Enter   发送内容" + Environment.NewLine +
                       "Ctrl+↑ / ↓   翻看发送历史" + Environment.NewLine +
                       "Ctrl+C       复制选中的日志" + Environment.NewLine +
                       Environment.NewLine +
                       "小贴士" + Environment.NewLine +
                       "· HEX 示例：48 65 6C 6C 6F" + Environment.NewLine +
                       "· 服务端可只发给某客户端"
            };

            _tipsCard.Body.Controls.Add(tips);
            _sideLayout.Controls.Add(_tipsCard, 0, 1);

            return _sideLayout;
        }

        private Card BuildSendCard()
        {
            _sendCard = new Card("发送");
            _sendCard.Body.Padding = new Padding(12, 10, 12, 10);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Theme.Surface,
                Margin = Padding.Empty
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            _sendInput = new InputField(true)
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                PlaceholderText = "输入要发送的内容。HEX 模式下可输入：48 65 6C 6C 6F"
            };
            layout.Controls.Add(_sendInput, 0, 0);

            var bottom = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Theme.Surface,
                Margin = Padding.Empty
            };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            _sendButton = new FlatButton
            {
                Text = "发送  Ctrl+Enter",
                Style = FlatButton.ButtonStyle.Primary,
                AutoWidth = true,
                Height = 32,
                Margin = new Padding(0, 8, 8, 0)
            };

            _clearSendButton = new FlatButton
            {
                Text = "清空",
                Style = FlatButton.ButtonStyle.Ghost,
                AutoWidth = true,
                Height = 32,
                Margin = new Padding(0, 8, 0, 0)
            };

            _payloadLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "0 字节",
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Theme.TextFaint,
                Font = Theme.UiSmall,
                Margin = new Padding(0, 8, 0, 0)
            };

            bottom.Controls.Add(_sendButton, 0, 0);
            bottom.Controls.Add(_clearSendButton, 1, 0);
            bottom.Controls.Add(_payloadLabel, 2, 0);
            layout.Controls.Add(bottom, 0, 1);

            _sendCard.Body.Controls.Add(layout);

            _targetMenu = new MenuButton { Visible = false, Margin = new Padding(0, 5, 8, 0) };
            _encodingMenu = new MenuButton { Margin = new Padding(0, 5, 8, 0) };
            _hexSendToggle = new ToggleSwitch { Margin = new Padding(0, 8, 4, 0) };
            _crlfToggle = new ToggleSwitch { Margin = new Padding(0, 8, 4, 0) };
            _autoSendToggle = new ToggleSwitch { Margin = new Padding(0, 8, 4, 0) };
            _intervalField = new InputField
            {
                Size = new Size(62, 28),
                Margin = new Padding(0, 4, 4, 0)
            };
            _intervalField.Value = "1000";

            _sendCard.AddTool(_targetMenu, 6);
            _sendCard.AddTool(_encodingMenu, 12);
            _sendCard.AddTool(CreateToolLabel("HEX"), 4);
            _sendCard.AddTool(_hexSendToggle, 16);
            _sendCard.AddTool(CreateToolLabel("CRLF"), 4);
            _sendCard.AddTool(_crlfToggle, 16);
            _sendCard.AddTool(CreateToolLabel("自动发送"), 4);
            _sendCard.AddTool(_autoSendToggle, 6);
            _sendCard.AddTool(_intervalField, 4);
            _sendCard.AddTool(CreateToolLabel("ms"), 0);

            return _sendCard;
        }

        private Control BuildStatusBar()
        {
            var bar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Theme.Background,
                Margin = Padding.Empty
            };
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var hint = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Ctrl+Enter 发送 · Ctrl+↑ / Ctrl+↓ 翻发送历史 · ← 接收  → 发送",
                ForeColor = Theme.TextFaint,
                Font = Theme.UiSmall,
                TextAlign = ContentAlignment.MiddleLeft
            };

            _countersLabel = new Label
            {
                AutoSize = true,
                Text = "接收 0 B · 发送 0 B",
                ForeColor = Theme.TextFaint,
                Font = Theme.UiSmall,
                TextAlign = ContentAlignment.MiddleRight
            };

            bar.Controls.Add(hint, 0, 0);
            bar.Controls.Add(_countersLabel, 1, 0);
            return bar;
        }

        private static Label CreateFieldLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = false,
                Width = 40,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Theme.TextMuted,
                Font = Theme.UiSmall,
                Margin = new Padding(4, 0, 4, 0)
            };
        }

        private static Label CreateToolLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = Theme.TextMuted,
                Font = Theme.UiSmall,
                Margin = new Padding(0, 10, 2, 0)
            };
        }

        private static Label CreateInfoKey(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Theme.TextFaint,
                Font = Theme.UiSmall,
                Margin = Padding.Empty
            };
        }

        private static Label CreateInfoValue(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Theme.Text,
                Font = Theme.UiSmall,
                AutoEllipsis = true,
                Margin = Padding.Empty
            };
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            SetupSplitters();

            DarkMode.ApplyTitleBar(this);
            DarkMode.ApplyControl(_log.Inner);
            DarkMode.ApplyControl(_sendInput.Box);
            DarkMode.ApplyControl(_clientList);

            if (_options.AutoConnect)
            {
                BeginInvoke(new Action(BeginConnect));
            }

            BeginInvoke(new Action(() => _sendInput.Box.Focus()));
        }

        private void SetupSplitters()
        {
            // SplitContainer 必须等控件有了实际尺寸后再设置分隔位置和最小尺寸，
            // 否则会抛 “SplitterDistance 必须在 Panel1MinSize 和 Width - Panel2MinSize 之间”。
            var contentWidth = _contentSplit.Width;
            if (contentWidth > 0)
            {
                var distance = Math.Max(320, contentWidth - 296);
                distance = Math.Min(distance, Math.Max(320, contentWidth - 30));
                try
                {
                    _contentSplit.SplitterDistance = distance;
                    _contentSplit.Panel1MinSize = Math.Min(320, distance);
                    _contentSplit.Panel2MinSize = Math.Min(240, Math.Max(0, contentWidth - distance - _contentSplit.SplitterWidth));
                }
                catch (InvalidOperationException)
                {
                }
            }

            var mainHeight = _mainSplit.Height;
            if (mainHeight > 0)
            {
                var distance = Math.Max(240, mainHeight - 214);
                distance = Math.Min(distance, Math.Max(240, mainHeight - 30));
                try
                {
                    _mainSplit.SplitterDistance = distance;
                    _mainSplit.Panel1MinSize = Math.Min(240, distance);
                    _mainSplit.Panel2MinSize = Math.Min(150, Math.Max(0, mainHeight - distance - _mainSplit.SplitterWidth));
                }
                catch (InvalidOperationException)
                {
                }
            }
        }
    }
}
