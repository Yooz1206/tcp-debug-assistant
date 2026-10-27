using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TcpDebug.Core.Net
{
    /// <summary>
    /// TCP 客户端连接。
    /// </summary>
    public sealed class TcpClientLink : IDisposable
    {
        private const int ReceiveBufferSize = 16 * 1024;

        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        private Socket _socket;
        private Task _receiveTask;
        private int _closedRaised;
        private volatile bool _closing;

        public event Action<byte[]> DataReceived;

        /// <summary>连接断开时触发；主动断开时参数为 null。</summary>
        public event Action<string> Closed;

        public bool IsConnected
        {
            get
            {
                var socket = _socket;
                return socket != null && socket.Connected;
            }
        }

        public string RemoteEndPoint { get; private set; }

        public string LocalEndPoint { get; private set; }

        public async Task ConnectAsync(string host, int port, TimeSpan timeout)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new ArgumentException("请填写目标地址。", "host");
            }

            if (port < 1 || port > 65535)
            {
                throw new ArgumentOutOfRangeException("port", "端口范围是 1 - 65535。");
            }

            if (_socket != null)
            {
                throw new InvalidOperationException("当前已经处于连接状态，请先断开。");
            }

            if (timeout <= TimeSpan.Zero)
            {
                timeout = TimeSpan.FromSeconds(5);
            }

            IPAddress parsed;
            IPAddress[] addresses;
            if (IPAddress.TryParse(host, out parsed))
            {
                addresses = new[] { parsed };
            }
            else
            {
                addresses = await Dns.GetHostAddressesAsync(host).ConfigureAwait(false);
            }

            if (addresses.Length == 0)
            {
                throw new SocketException((int)SocketError.HostNotFound);
            }

            Exception lastError = null;
            foreach (var address in addresses)
            {
                var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    var connectTask = socket.ConnectAsync(address, port);
                    var finished = await Task.WhenAny(connectTask, Task.Delay(timeout)).ConfigureAwait(false);
                    if (finished != connectTask)
                    {
                        throw new TimeoutException(
                            string.Format("连接 {0}:{1} 超时（{2:0} 秒）。", host, port, timeout.TotalSeconds));
                    }

                    await connectTask.ConfigureAwait(false);

                    Configure(socket);
                    RemoteEndPoint = FormatEndPoint(socket.RemoteEndPoint);
                    LocalEndPoint = FormatEndPoint(socket.LocalEndPoint);
                    Interlocked.Exchange(ref _closedRaised, 0);
                    _closing = false;
                    _socket = socket;
                    _receiveTask = Task.Run(new Func<Task>(ReceiveLoopAsync));
                    return;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    try
                    {
                        socket.Dispose();
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            throw lastError;
        }

        public async Task SendAsync(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            var socket = _socket;
            if (socket == null)
            {
                throw new InvalidOperationException("尚未连接。");
            }

            await _sendLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await NetErrors.SendAllAsync(socket, data).ConfigureAwait(false);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public async Task CloseAsync()
        {
            _closing = true;
            var socket = Interlocked.Exchange(ref _socket, null);
            if (socket != null)
            {
                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                }
                catch (Exception)
                {
                }

                try
                {
                    socket.Dispose();
                }
                catch (Exception)
                {
                }
            }

            var receiveTask = _receiveTask;
            _receiveTask = null;
            if (receiveTask != null)
            {
                try
                {
                    await receiveTask.ConfigureAwait(false);
                }
                catch (Exception)
                {
                }
            }

            RaiseClosed(null);
        }

        public void Dispose()
        {
            // 关闭窗口时不希望再回调 UI，因此这里不触发 Closed。
            _closing = true;
            var socket = Interlocked.Exchange(ref _socket, null);
            if (socket != null)
            {
                try
                {
                    socket.Dispose();
                }
                catch (Exception)
                {
                }
            }
        }

        private async Task ReceiveLoopAsync()
        {
            var buffer = new byte[ReceiveBufferSize];
            try
            {
                while (true)
                {
                    var read = await _socket
                        .ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None)
                        .ConfigureAwait(false);

                    if (read <= 0)
                    {
                        RaiseClosed(_closing ? null : "对端已关闭连接。");
                        return;
                    }

                    var chunk = new byte[read];
                    Buffer.BlockCopy(buffer, 0, chunk, 0, read);

                    var handler = DataReceived;
                    if (handler != null)
                    {
                        handler(chunk);
                    }
                }
            }
            catch (Exception ex)
            {
                RaiseClosed(_closing ? null : NetErrors.Describe(ex));
            }
        }

        private void RaiseClosed(string reason)
        {
            if (Interlocked.Exchange(ref _closedRaised, 1) != 0)
            {
                return;
            }

            var socket = Interlocked.Exchange(ref _socket, null);
            if (socket != null)
            {
                try
                {
                    socket.Dispose();
                }
                catch (Exception)
                {
                }
            }

            var handler = Closed;
            if (handler != null)
            {
                handler(reason);
            }
        }

        private static void Configure(Socket socket)
        {
            socket.NoDelay = true;
            try
            {
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            }
            catch (SocketException)
            {
            }
        }

        private static string FormatEndPoint(EndPoint endPoint)
        {
            return endPoint == null ? "未知" : endPoint.ToString();
        }
    }
}
