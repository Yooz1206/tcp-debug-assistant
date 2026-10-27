using System;
using System.Net;
using System.Net.Sockets;
using TcpDebug.Core.Net;

namespace TcpDebug.Core.Tests
{
    public static class TcpLinkTests
    {
        public static void Register()
        {
            TestRunner.Add("客户端与服务端可以互发数据", () =>
            {
                using (var server = new TcpServerLink())
                using (var client = new TcpClientLink())
                {
                    var sessionReady = Signals.New<TcpSession>();
                    var serverGot = Signals.New<byte[]>();
                    server.ClientConnected += session => sessionReady.TrySetResult(session);
                    server.DataReceived += (session, data) => serverGot.TrySetResult(data);

                    Signals.Await(server.StartAsync("127.0.0.1", 0));
                    Assert.True(server.Port > 0);

                    var clientGot = Signals.New<byte[]>();
                    client.DataReceived += data => clientGot.TrySetResult(data);

                    Signals.Await(client.ConnectAsync("127.0.0.1", server.Port, TimeSpan.FromSeconds(5)));
                    Assert.True(client.IsConnected);
                    Assert.NotNull(client.RemoteEndPoint);

                    var activeSession = Signals.Await(sessionReady.Task);
                    Assert.NotNull(activeSession);

                    Signals.Await(client.SendAsync(new byte[] { 0x01, 0x02, 0x03 }));
                    Assert.SequenceEqual(new byte[] { 0x01, 0x02, 0x03 }, Signals.Await(serverGot.Task));

                    Signals.Await(activeSession.SendAsync(new byte[] { 0xAA, 0xBB }));
                    Assert.SequenceEqual(new byte[] { 0xAA, 0xBB }, Signals.Await(clientGot.Task));
                }
            });

            TestRunner.Add("服务端维护会话列表", () =>
            {
                using (var server = new TcpServerLink())
                using (var client = new TcpClientLink())
                {
                    var connected = Signals.New<TcpSession>();
                    server.ClientConnected += session => connected.TrySetResult(session);
                    Signals.Await(server.StartAsync("127.0.0.1", 0));

                    Signals.Await(client.ConnectAsync("127.0.0.1", server.Port, TimeSpan.FromSeconds(5)));
                    Signals.Await(connected.Task);

                    Assert.Equal(1, server.Sessions.Count);
                    Assert.True(server.IsListening);
                }
            });

            TestRunner.Add("客户端断开后服务端收到通知并移除会话", () =>
            {
                using (var server = new TcpServerLink())
                using (var client = new TcpClientLink())
                {
                    var connected = Signals.New<TcpSession>();
                    var disconnected = Signals.New<TcpSession>();
                    server.ClientConnected += session => connected.TrySetResult(session);
                    server.ClientDisconnected += (session, reason) => disconnected.TrySetResult(session);
                    Signals.Await(server.StartAsync("127.0.0.1", 0));

                    Signals.Await(client.ConnectAsync("127.0.0.1", server.Port, TimeSpan.FromSeconds(5)));
                    var activeSession = Signals.Await(connected.Task);

                    Signals.Await(client.CloseAsync());
                    var gone = Signals.Await(disconnected.Task);
                    Assert.Equal(activeSession.Id, gone.Id);
                    Assert.Equal(0, server.Sessions.Count);
                }
            });

            TestRunner.Add("服务端停止后客户端收到断开通知", () =>
            {
                using (var server = new TcpServerLink())
                using (var client = new TcpClientLink())
                {
                    var connected = Signals.New<TcpSession>();
                    var closed = Signals.New<string>();
                    server.ClientConnected += session => connected.TrySetResult(session);
                    client.Closed += reason => closed.TrySetResult(reason ?? "closed");
                    Signals.Await(server.StartAsync("127.0.0.1", 0));

                    Signals.Await(client.ConnectAsync("127.0.0.1", server.Port, TimeSpan.FromSeconds(5)));
                    Signals.Await(connected.Task);

                    Signals.Await(server.StopAsync());

                    var closeReason = Signals.Await(closed.Task);
                    Assert.NotNull(closeReason);
                    Assert.Equal(0, server.Sessions.Count);
                    Assert.False(server.IsListening);
                }
            });

            TestRunner.Add("连接被拒绝时抛出 SocketException", () =>
            {
                var probe = new TcpListener(IPAddress.Loopback, 0);
                probe.Start();
                var port = ((IPEndPoint)probe.LocalEndpoint).Port;
                probe.Stop();

                using (var client = new TcpClientLink())
                {
                    Assert.Throws<SocketException>(
                        () => Signals.Await(client.ConnectAsync("127.0.0.1", port, TimeSpan.FromSeconds(3))));
                    Assert.False(client.IsConnected);
                }
            });
        }
    }
}
