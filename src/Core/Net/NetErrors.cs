using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TcpDebug.Core.Net
{
    /// <summary>
    /// 网络层的公共小工具：统一的中文错误描述和发送循环。
    /// </summary>
    internal static class NetErrors
    {
        public static string Describe(Exception ex)
        {
            var socketException = ex as SocketException;
            if (socketException != null)
            {
                switch (socketException.SocketErrorCode)
                {
                    case SocketError.ConnectionReset:
                        return "连接被对方重置。";
                    case SocketError.ConnectionAborted:
                        return "连接被中止。";
                    case SocketError.Shutdown:
                        return "连接已关闭。";
                    case SocketError.TimedOut:
                        return "网络超时。";
                    case SocketError.HostUnreachable:
                        return "无法访问目标主机。";
                    case SocketError.NetworkUnreachable:
                        return "网络不可达。";
                    case SocketError.ConnectionRefused:
                        return "目标端口拒绝连接。";
                    default:
                        return "网络错误：" + socketException.Message;
                }
            }

            if (ex is ObjectDisposedException)
            {
                return "连接已断开。";
            }

            if (ex is IOException)
            {
                return ex.Message;
            }

            return ex.Message;
        }

        public static async Task SendAllAsync(Socket socket, byte[] data)
        {
            var offset = 0;
            while (offset < data.Length)
            {
                var sent = await socket
                    .SendAsync(new ArraySegment<byte>(data, offset, data.Length - offset), SocketFlags.None)
                    .ConfigureAwait(false);

                if (sent <= 0)
                {
                    throw new IOException("发送失败：连接已关闭。");
                }

                offset += sent;
            }
        }
    }
}
