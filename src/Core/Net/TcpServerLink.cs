using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TcpDebug.Core.Net
{
    /// <summary>
    /// TCP 服务端：监听端口并接受多个客户端连接。
    /// </summary>
    public sealed class TcpServerLink : IDisposable
    {
        private readonly object _gate = new object();
        private readonly List<TcpSession> _sessions = new List<TcpSession>();
        private TcpListener _listener;
        private CancellationTokenSource _cancellation;
        private Task _acceptTask;

        public event Action<TcpSession> ClientConnected;

        public event Action<TcpSession, string> ClientDisconnected;

        public event Action<TcpSession, byte[]> DataReceived;

        public int Port { get; private set; }

        public bool IsListening
        {
            get { return _listener != null; }
        }

        public IReadOnlyList<TcpSession> Sessions
        {
            get
            {
                lock (_gate)
                {
                    return _sessions.ToArray();
                }
            }
        }

        public Task StartAsync(string host, int port)
        {
            if (_listener != null)
            {
                throw new InvalidOperationException("服务端已经在监听中。");
            }

            if (port < 0 || port > 65535)
            {
                throw new ArgumentOutOfRangeException("port", "端口范围是 0 - 65535。");
            }

            IPAddress address;
            if (!IPAddress.TryParse(host, out address))
            {
                throw new ArgumentException("监听地址必须是本机 IP，例如 127.0.0.1 或 0.0.0.0。", "host");
            }

            var listener = new TcpListener(address, port);
            listener.Start();

            _listener = listener;
            Port = ((IPEndPoint)listener.LocalEndpoint).Port;
            _cancellation = new CancellationTokenSource();
            var token = _cancellation.Token;
            _acceptTask = Task.Run(() => AcceptLoopAsync(listener, token));
            return Task.FromResult(0);
        }

        public async Task StopAsync()
        {
            var listener = _listener;
            _listener = null;

            if (listener != null)
            {
                try
                {
                    listener.Stop();
                }
                catch (Exception)
                {
                }
            }

            if (_cancellation != null)
            {
                _cancellation.Cancel();
            }

            var acceptTask = _acceptTask;
            _acceptTask = null;
            if (acceptTask != null)
            {
                try
                {
                    await acceptTask.ConfigureAwait(false);
                }
                catch (Exception)
                {
                }
            }

            TcpSession[] sessions;
            lock (_gate)
            {
                sessions = _sessions.ToArray();
                _sessions.Clear();
            }

            foreach (var session in sessions)
            {
                session.Dispose();
            }

            if (_cancellation != null)
            {
                _cancellation.Dispose();
                _cancellation = null;
            }
        }

        public void Dispose()
        {
            try
            {
                StopAsync().Wait(TimeSpan.FromSeconds(2));
            }
            catch (Exception)
            {
            }
        }

        private async Task AcceptLoopAsync(TcpListener listener, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Socket socket;
                try
                {
                    socket = await listener.AcceptSocketAsync().ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (SocketException)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    continue;
                }

                var session = new TcpSession(socket);
                session.DataReceived += OnSessionData;
                session.Closed += OnSessionClosed;

                lock (_gate)
                {
                    _sessions.Add(session);
                }

                var connected = ClientConnected;
                if (connected != null)
                {
                    connected(session);
                }

                session.StartReceiving();
            }
        }

        private void OnSessionData(TcpSession session, byte[] data)
        {
            var handler = DataReceived;
            if (handler != null)
            {
                handler(session, data);
            }
        }

        private void OnSessionClosed(TcpSession session, string reason)
        {
            lock (_gate)
            {
                _sessions.Remove(session);
            }

            var handler = ClientDisconnected;
            if (handler != null)
            {
                handler(session, reason);
            }
        }
    }
}
