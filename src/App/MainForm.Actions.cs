using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TcpDebug.App.Controls;
using TcpDebug.Core;
using TcpDebug.Core.Net;

namespace TcpDebug.App
{
    public sealed partial class MainForm
    {
        private Encoding _displayEncoding = Encoding.UTF8;
        private volatile bool _hexView;

        private void WireEvents()
        {
            _modeSwitch.SelectionChanged += (sender, args) => ApplyMode();
            _connectButton.Click += (sender, args) => BeginConnect();
            _disconnectButton.Click += async (sender, args) => await DisconnectAsync();

            _sendButton.Click += (sender, args) => SendCurrent(false);
            _clearSendButton.Click += (sender, args) =>
            {
                _sendInput.Value = string.Empty;
                _sendInput.Box.Focus();
            };

            _sendInput.Box.TextChanged += (sender, args) => UpdatePayloadHint();

            _hexSendToggle.CheckedChanged += (sender, args) => UpdatePayloadHint();
            _crlfToggle.CheckedChanged += (sender, args) => UpdatePayloadHint();
            _autoSendToggle.CheckedChanged += (sender, args) => UpdateAutoSend();
            _intervalField.Box.KeyPress += DigitOnlyKeyPress;
            _intervalField.Box.TextChanged += (sender, args) =>
            {
                if (_autoSendToggle.Checked)
                {
                    UpdateAutoSend();
                }
            };

            _timestampsToggle.CheckedChanged += (sender, args) => _log.ShowTimestamps = _timestampsToggle.Checked;
            _pauseToggle.CheckedChanged += (sender, args) => _log.Paused = _pauseToggle.Checked;
            _clearLogButton.Click += (sender, args) =>
            {
                _log.Clear();
                _stats.Reset();
                UpdateCounters();
            };
            _saveLogButton.Click += (sender, args) => SaveLog();
            _log.SaveRequested += (sender, args) => SaveLog();

            _viewSwitch.SelectionChanged += (sender, args) => _hexView = _viewSwitch.SelectedIndex == 1;

            _portField.Box.KeyPress += DigitOnlyKeyPress;

            _client.DataReceived += OnClientData;
            _client.Closed += OnClientClosed;
            _server.ClientConnected += OnServerClientConnected;
            _server.ClientDisconnected += OnServerClientDisconnected;
            _server.DataReceived += OnServerData;

            FormClosing += OnFormClosing;
        }

        private void ApplyMode()
        {
            _isServerMode = _modeSwitch.SelectedIndex == 1;

            _connectButton.Text = _isServerMode ? "开始监听" : "连接";
            _disconnectButton.Text = _isServerMode ? "停止监听" : "断开";
            _targetMenu.Visible = _isServerMode;
            _clientsCard.Visible = _isServerMode;
            _tipsCard.Visible = !_isServerMode;
            _infoCard.Title = _isServerMode ? "监听信息" : "连接信息";

            if (_isServerMode)
            {
                if (_hostField.Value.Length == 0 || _hostField.Value == "127.0.0.1")
                {
                    _hostField.Value = "0.0.0.0";
                }
            }
            else if (_hostField.Value == "0.0.0.0")
            {
                _hostField.Value = "127.0.0.1";
            }

            SetStatus(_isServerMode ? "未监听" : "未连接", Theme.TextFaint);
            SetInfo(_isServerMode ? "未监听" : "未连接", null, null, false);
            UpdateTargetMenu();
        }

        private void BeginConnect()
        {
            if (_busy || HasConnection)
            {
                return;
            }

            if (_isServerMode)
            {
                StartServer();
            }
            else
            {
                ConnectClient();
            }
        }

        private async void ConnectClient()
        {
            var host = _hostField.Value.Trim();
            int port;

            if (host.Length == 0)
            {
                _log.Append(LogKind.Error, "请填写目标地址。");
                _hostField.Box.Focus();
                return;
            }

            if (!TryReadPort(1, out port))
            {
                _log.Append(LogKind.Error, "端口必须是 1 - 65535 之间的数字。");
                _portField.Box.Focus();
                return;
            }

            SetBusy(true);
            SetStatus("正在连接…", Theme.Warning);
            _log.Append(LogKind.Info, "正在连接 " + host + ":" + port + " …");

            try
            {
                await _client.ConnectAsync(host, port, TimeSpan.FromSeconds(5));
                OnConnected();
            }
            catch (Exception ex)
            {
                _log.Append(LogKind.Error, "连接失败：" + ex.Message);
                SetStatus("未连接", Theme.TextFaint);
                SetInfo("未连接", null, null, false);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void StartServer()
        {
            var host = _hostField.Value.Trim();
            int port;

            if (!TryReadPort(0, out port))
            {
                _log.Append(LogKind.Error, "端口必须是 0 - 65535 之间的数字。");
                _portField.Box.Focus();
                return;
            }

            IPAddress address;
            if (!IPAddress.TryParse(host, out address))
            {
                _log.Append(LogKind.Error, "监听地址请填写本机 IP，例如 0.0.0.0 或 127.0.0.1。");
                _hostField.Box.Focus();
                return;
            }

            SetBusy(true);
            try
            {
                await _server.StartAsync(host, port);
                _portField.Value = _server.Port.ToString();

                _connectedAt = DateTime.Now;
                SetStatus("监听中", Theme.Success);
                SetInfo("监听中", host + ":" + _server.Port, null, true);
                _connectButton.Enabled = false;
                _disconnectButton.Enabled = true;
                _modeSwitch.Enabled = false;
                _hostField.Enabled = false;
                _portField.Enabled = false;
                _log.Append(LogKind.Info, string.Format("开始监听 {0}:{1}，等待客户端连接…", host, _server.Port));
            }
            catch (Exception ex)
            {
                _log.Append(LogKind.Error, "监听失败：" + ex.Message);
                SetStatus("未连接", Theme.TextFaint);
                SetInfo("未监听", null, null, false);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void OnConnected()
        {
            _connectedAt = DateTime.Now;
            SetStatus("已连接", Theme.Success);
            SetInfo("已连接", _client.LocalEndPoint, _client.RemoteEndPoint, true);
            _connectButton.Enabled = false;
            _disconnectButton.Enabled = true;
            _modeSwitch.Enabled = false;
            _hostField.Enabled = false;
            _portField.Enabled = false;
            _log.Append(LogKind.Info, "已连接到 " + _client.RemoteEndPoint + "。");
            _sendInput.Box.Focus();
            UpdateCounters();
        }

        private async Task DisconnectAsync()
        {
            if (_busy)
            {
                return;
            }

            SetBusy(true);
            try
            {
                if (_server.IsListening)
                {
                    await _server.StopAsync();
                    _sessions.Clear();
                    _sessionOrder.Clear();
                    RefreshClientList();
                    _log.Append(LogKind.Info, "已停止监听。");
                }

                if (_client.IsConnected)
                {
                    await _client.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                _log.Append(LogKind.Error, "断开时出错：" + ex.Message);
            }
            finally
            {
                ResetConnectionUi();
                SetStatus("未连接", Theme.TextFaint);
                SetInfo(_isServerMode ? "未监听" : "未连接", null, null, false);
                SetBusy(false);
            }
        }

        private void ResetConnectionUi()
        {
            _connectButton.Enabled = true;
            _disconnectButton.Enabled = false;
            _modeSwitch.Enabled = true;
            _hostField.Enabled = true;
            _portField.Enabled = true;
            _connectedAt = default(DateTime);
            _durationValue.Text = "—";
            StopAutoSend();
            UpdateCounters();
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            _connectButton.Enabled = !busy && !HasConnection;
            _disconnectButton.Enabled = !busy && HasConnection;
            _modeSwitch.Enabled = !busy && !HasConnection;
            Cursor = busy ? Cursors.AppStarting : Cursors.Default;
        }

        private void OnClientData(byte[] data)
        {
            _stats.RecordReceived(data.Length);
            _log.Append(LogKind.Received, FormatPayload(data), data.Length);
        }

        private void OnClientClosed(string reason)
        {
            Ui(() =>
            {
                if (reason == null)
                {
                    _log.Append(LogKind.Info, "已断开连接。");
                }
                else
                {
                    _log.Append(LogKind.Error, "连接已断开：" + reason);
                }

                ResetConnectionUi();
                SetStatus("未连接", Theme.TextFaint);
                SetInfo("未连接", null, null, false);
            });
        }

        private void OnServerClientConnected(TcpSession session)
        {
            Ui(() =>
            {
                if (!_sessions.ContainsKey(session.Id))
                {
                    _sessions[session.Id] = session;
                    _sessionOrder.Add(session.Id);
                }

                RefreshClientList();
                _log.Append(LogKind.Info, "客户端 " + session.RemoteEndPoint + " 已连接。");
            });
        }

        private void OnServerClientDisconnected(TcpSession session, string reason)
        {
            Ui(() =>
            {
                _sessions.Remove(session.Id);
                _sessionOrder.Remove(session.Id);
                RefreshClientList();
                _log.Append(LogKind.Info, "客户端 " + session.RemoteEndPoint + " 已断开" +
                                           (string.IsNullOrEmpty(reason) ? "。" : "：" + reason));
            });
        }

        private void OnServerData(TcpSession session, byte[] data)
        {
            _stats.RecordReceived(data.Length);
            _log.Append(LogKind.Received, "[" + session.RemoteEndPoint + "] " + FormatPayload(data), data.Length);
        }

        private void SendCurrent(bool automatic)
        {
            if (_busy)
            {
                return;
            }

            byte[] payload;
            try
            {
                payload = BuildPayload();
            }
            catch (FormatException ex)
            {
                _log.Append(LogKind.Error, "HEX 内容有误：" + ex.Message);
                StopAutoSend();
                return;
            }
            catch (ArgumentException ex)
            {
                _log.Append(LogKind.Error, ex.Message);
                StopAutoSend();
                return;
            }

            if (payload.Length == 0)
            {
                if (!automatic)
                {
                    _log.Append(LogKind.Notice, "没有可发送的内容。");
                }

                return;
            }

            if (_isServerMode)
            {
                var targets = GetSendTargets();
                if (targets.Count == 0)
                {
                    _log.Append(LogKind.Notice, "还没有客户端连接，无法发送。");
                    StopAutoSend();
                    return;
                }

                foreach (var session in targets)
                {
                    SendToSession(session, payload);
                }
            }
            else
            {
                if (!_client.IsConnected)
                {
                    _log.Append(LogKind.Notice, "尚未连接，无法发送。");
                    StopAutoSend();
                    return;
                }

                SendToClient(payload);
            }

            _history.Add(_sendInput.Value);
        }

        private async void SendToClient(byte[] payload)
        {
            try
            {
                await _client.SendAsync(payload);
                _stats.RecordSent(payload.Length);
                _log.Append(LogKind.Sent, FormatPayload(payload), payload.Length);
            }
            catch (Exception ex)
            {
                _log.Append(LogKind.Error, "发送失败：" + ex.Message);
            }
        }

        private async void SendToSession(TcpSession session, byte[] payload)
        {
            try
            {
                await session.SendAsync(payload);
                _stats.RecordSent(payload.Length);
                _log.Append(LogKind.Sent, "[" + session.RemoteEndPoint + "] " + FormatPayload(payload), payload.Length);
            }
            catch (Exception ex)
            {
                _log.Append(LogKind.Error, "发送到 " + session.RemoteEndPoint + " 失败：" + ex.Message);
            }
        }

        private byte[] BuildPayload()
        {
            var text = _sendInput.Value;
            var body = _hexSendToggle.Checked
                ? HexCodec.Parse(text)
                : CurrentEncoding().GetBytes(text);

            if (!_crlfToggle.Checked)
            {
                return body;
            }

            var withCrlf = new byte[body.Length + 2];
            Array.Copy(body, withCrlf, body.Length);
            withCrlf[body.Length] = 0x0D;
            withCrlf[body.Length + 1] = 0x0A;
            return withCrlf;
        }

        private Encoding CurrentEncoding()
        {
            try
            {
                return DataEncoders.Get(_encodingMenu.SelectedOption);
            }
            catch (ArgumentException)
            {
                return Encoding.UTF8;
            }
        }

        private string FormatPayload(byte[] data)
        {
            return _hexView ? HexDumpFormatter.Format(data) : _displayEncoding.GetString(data);
        }

        private List<TcpSession> GetSendTargets()
        {
            var result = new List<TcpSession>();
            var selected = _targetMenu.SelectedOption;

            foreach (var id in _sessionOrder)
            {
                if (!_sessions.ContainsKey(id))
                {
                    continue;
                }

                var session = _sessions[id];
                if (string.IsNullOrEmpty(selected) || selected == "全部客户端" || session.RemoteEndPoint == selected)
                {
                    result.Add(session);
                }
            }

            return result;
        }

        private void UpdatePayloadHint()
        {
            try
            {
                var size = BuildPayload().Length;
                _payloadLabel.ForeColor = Theme.TextFaint;
                _payloadLabel.Text = ByteSize.Format(size) +
                                     (_hexSendToggle.Checked ? " · HEX" : string.Empty);
            }
            catch (FormatException)
            {
                _payloadLabel.ForeColor = Theme.Danger;
                _payloadLabel.Text = "HEX 格式有误";
            }
            catch (ArgumentException)
            {
                _payloadLabel.ForeColor = Theme.Danger;
                _payloadLabel.Text = "编码不可用";
            }
        }

        private void UpdateAutoSend()
        {
            if (_autoSendUpdating)
            {
                return;
            }

            _autoSendUpdating = true;
            try
            {
                _autoSendTimer.Stop();
                if (!_autoSendToggle.Checked)
                {
                    return;
                }

                int interval;
                if (!int.TryParse(_intervalField.Value.Trim(), out interval) || interval < 50)
                {
                    _autoSendToggle.Checked = false;
                    _log.Append(LogKind.Error, "自动发送间隔需要是 ≥ 50 的毫秒数。");
                    return;
                }

                _autoSendTimer.Interval = interval;
                _autoSendTimer.Start();
                _log.Append(LogKind.Notice, "自动发送已开启：每 " + interval + " 毫秒一次。");
            }
            finally
            {
                _autoSendUpdating = false;
            }
        }

        private void StopAutoSend()
        {
            _autoSendTimer.Stop();
            if (!_autoSendToggle.Checked)
            {
                return;
            }

            _autoSendUpdating = true;
            _autoSendToggle.Checked = false;
            _autoSendUpdating = false;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // 用 ProcessCmdKey 处理全局快捷键：它拿到的 keyData 带有可靠的修饰键状态，
            // 不受多行 TextBox 自行处理 Enter 的影响。
            if (keyData == (Keys.Control | Keys.Enter))
            {
                SendCurrent(false);
                return true;
            }

            if (keyData == (Keys.Control | Keys.Up))
            {
                var previous = _history.Previous();
                if (previous != null)
                {
                    SetSendText(previous);
                }

                return true;
            }

            if (keyData == (Keys.Control | Keys.Down))
            {
                var next = _history.Next();
                if (next != null)
                {
                    SetSendText(next);
                }

                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void SetSendText(string text)
        {
            _sendInput.Value = text;
            _sendInput.Box.SelectionStart = _sendInput.Box.TextLength;
            _sendInput.Box.Focus();
        }

        private void RefreshClientList()
        {
            _clientList.BeginUpdate();
            _clientList.Items.Clear();
            foreach (var id in _sessionOrder)
            {
                if (_sessions.ContainsKey(id))
                {
                    _clientList.Items.Add(_sessions[id].RemoteEndPoint);
                }
            }

            _clientList.EndUpdate();

            _clientsCard.Title = "客户端 (" + _sessions.Count + ")";
            _clientEmptyLabel.Visible = _sessions.Count == 0;
            UpdateTargetMenu();
        }

        private void UpdateTargetMenu()
        {
            var options = new List<string> { "全部客户端" };
            foreach (var id in _sessionOrder)
            {
                if (_sessions.ContainsKey(id))
                {
                    options.Add(_sessions[id].RemoteEndPoint);
                }
            }

            var selected = _targetMenu.SelectedOption;
            if (string.IsNullOrEmpty(selected) || !options.Contains(selected))
            {
                selected = "全部客户端";
            }

            _targetMenu.SetOptions("发送到", options, selected, null);
        }

        private void OnClientListDrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _clientList.Items.Count)
            {
                return;
            }

            var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            using (var brush = new SolidBrush(selected ? Theme.AccentSoft : Theme.Input))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            var text = Convert.ToString(_clientList.Items[e.Index]);
            TextRenderer.DrawText(e.Graphics, text, Theme.UiSmall,
                new Rectangle(e.Bounds.X + 8, e.Bounds.Y, Math.Max(0, e.Bounds.Width - 8), e.Bounds.Height),
                selected ? Theme.Text : Theme.TextMuted,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
        }

        private bool TryReadPort(int minimum, out int port)
        {
            port = 0;
            if (!int.TryParse(_portField.Value.Trim(), out port))
            {
                return false;
            }

            return port >= minimum && port <= 65535;
        }

        private void SetStatus(string text, Color color)
        {
            _statusPill.SetStatus(text, color);
        }

        private void SetInfo(string state, string local, string remote, bool active)
        {
            _stateValue.Text = state;
            _stateValue.ForeColor = active ? Theme.Success : Theme.Text;
            _localValue.Text = string.IsNullOrEmpty(local) ? "—" : local;
            _remoteValue.Text = string.IsNullOrEmpty(remote) ? "—" : remote;

            if (!active)
            {
                _connectedAt = default(DateTime);
                _durationValue.Text = "—";
            }
        }

        private void UpdateCounters()
        {
            _countersLabel.Text = string.Format("接收 {0} / {1} 包 · 发送 {2} / {3} 包",
                ByteSize.Format(_stats.BytesReceived), _stats.PacketsReceived,
                ByteSize.Format(_stats.BytesSent), _stats.PacketsSent);

            if (_connectedAt != default(DateTime))
            {
                var span = DateTime.Now - _connectedAt;
                _durationValue.Text = string.Format("{0:00}:{1:00}:{2:00}", span.Hours, span.Minutes, span.Seconds);
            }
        }

        private void SaveLog()
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "保存接收日志";
                dialog.Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*";
                dialog.FileName = "tcp-log-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt";

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    File.WriteAllText(dialog.FileName, _log.PlainText, new UTF8Encoding(false));
                    _log.Append(LogKind.Notice, "日志已保存到 " + dialog.FileName);
                }
                catch (Exception ex)
                {
                    _log.Append(LogKind.Error, "保存日志失败：" + ex.Message);
                }
            }
        }

        private void ApplySettings(AppSettings settings)
        {
            var workArea = Screen.PrimaryScreen.WorkingArea;
            var width = Math.Min(settings.Width, workArea.Width);
            var height = Math.Min(settings.Height, workArea.Height);
            if (width >= 900 && height >= 620)
            {
                Size = new Size(width, height);
            }

            _modeSwitch.SelectedIndex = settings.ServerMode ? 1 : 0;
            ApplyMode();

            _hostField.Value = settings.Host;
            _portField.Value = settings.Port.ToString();

            var encoding = DataEncoders.Names.Contains(settings.EncodingName) ? settings.EncodingName : "UTF-8";
            _encodingMenu.SetOptions("编码", DataEncoders.Names, encoding, option =>
            {
                _displayEncoding = DataEncoders.Get(option);
                UpdatePayloadHint();
            });
            _displayEncoding = DataEncoders.Get(encoding);

            _hexSendToggle.Checked = settings.HexSend;
            _crlfToggle.Checked = settings.AppendCrlf;
            _timestampsToggle.Checked = settings.ShowTimestamps;
            _log.ShowTimestamps = settings.ShowTimestamps;

            _viewSwitch.SelectedIndex = settings.HexView ? 1 : 0;
            _hexView = settings.HexView;

            _intervalField.Value = settings.AutoSendInterval.ToString();

            if (settings.Maximized)
            {
                WindowState = FormWindowState.Maximized;
            }

            UpdatePayloadHint();
        }

        private void ApplyOptions()
        {
            if (_options.ServerMode)
            {
                _modeSwitch.SelectedIndex = 1;
                ApplyMode();
            }

            if (!string.IsNullOrEmpty(_options.Host))
            {
                _hostField.Value = _options.Host;
            }

            if (_options.Port.HasValue)
            {
                _portField.Value = _options.Port.Value.ToString();
            }
        }

        private AppSettings CaptureSettings()
        {
            var settings = new AppSettings();
            settings.ServerMode = _isServerMode;
            settings.Host = _hostField.Value.Trim();
            settings.Port = 8080;

            int port;
            if (int.TryParse(_portField.Value.Trim(), out port))
            {
                settings.Port = port;
            }

            settings.EncodingName = _encodingMenu.SelectedOption;
            settings.HexSend = _hexSendToggle.Checked;
            settings.AppendCrlf = _crlfToggle.Checked;
            settings.ShowTimestamps = _timestampsToggle.Checked;
            settings.HexView = _viewSwitch.SelectedIndex == 1;

            int interval;
            settings.AutoSendInterval = int.TryParse(_intervalField.Value.Trim(), out interval) ? interval : 1000;

            var bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
            settings.Width = bounds.Width;
            settings.Height = bounds.Height;
            settings.Maximized = WindowState == FormWindowState.Maximized;
            return settings;
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                CaptureSettings().Save();
            }
            catch (Exception)
            {
            }

            _uiTimer.Stop();
            _autoSendTimer.Stop();
            _client.Dispose();
            _server.Dispose();
        }

        private void Ui(Action action)
        {
            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }

            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke(action);
                }
                else
                {
                    action();
                }
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void DigitOnlyKeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }
    }
}
