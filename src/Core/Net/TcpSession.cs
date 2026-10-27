using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TcpDebug.Core.Net
{
    /// <summary>
    /// 服务端接受到的一个客户端连接。
    /// </summary>
    public sealed class TcpSession : IDisposable
    {
        private const int ReceiveBufferSize = 16 * 1024;

        private readonly Socket _socket;
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
        private int _closed;

        internal TcpSession(Socket socket)
        {
            _socket = socket;
            try
            {
                _socket.NoDelay = true;
            }
            catch (SocketException)
            {
            }

            Id = Guid.NewGuid().ToString("N");
            RemoteEndPoint = FormatEndPoint(socket.RemoteEndPoint);
            LocalEndPoint = FormatEndPoint(socket.LocalEndPoint);
        }

        public string Id { get; private set; }

        public string RemoteEndPoint { get; private set; }

        public string LocalEndPoint { get; private set; }

        public bool IsClosed
        {
            get { return Volatile.Read(ref _closed) != 0; }
        }

        public event Action<TcpSession, byte[]> DataReceived;

        public event Action<TcpSession, string> Closed;

        internal void StartReceiving()
        {
            Task.Run(new Func<Task>(ReceiveLoopAsync));
        }

        public async Task SendAsync(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (IsClosed)
            {
                throw new InvalidOperationException("该客户端连接已关闭。");
            }

            await _sendLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await NetErrors.SendAllAsync(_socket, data).ConfigureAwait(false);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        internal void CloseNow(string reason)
        {
            var first = Interlocked.Exchange(ref _closed, 1) == 0;

            try
            {
                _socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception)
            {
            }

            try
            {
                _socket.Dispose();
            }
            catch (Exception)
            {
            }

            if (first && reason != null)
            {
                var handler = Closed;
                if (handler != null)
                {
                    handler(this, reason);
                }
            }
        }

        public void Dispose()
        {
            CloseNow(null);
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
                        CloseNow("对端已关闭连接。");
                        return;
                    }

                    var chunk = new byte[read];
                    Buffer.BlockCopy(buffer, 0, chunk, 0, read);

                    var handler = DataReceived;
                    if (handler != null)
                    {
                        handler(this, chunk);
                    }
                }
            }
            catch (Exception ex)
            {
                CloseNow(NetErrors.Describe(ex));
            }
        }

        private static string FormatEndPoint(EndPoint endPoint)
        {
            return endPoint == null ? "未知" : endPoint.ToString();
        }
    }
}
