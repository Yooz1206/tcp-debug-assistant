using System;
using System.Globalization;
using System.Threading;

namespace TcpDebug.Core
{
    /// <summary>
    /// 收发字节 / 报文计数器（线程安全）。
    /// </summary>
    public sealed class TransferStats
    {
        private long _bytesReceived;
        private long _bytesSent;
        private long _packetsReceived;
        private long _packetsSent;

        public long BytesReceived
        {
            get { return Interlocked.Read(ref _bytesReceived); }
        }

        public long BytesSent
        {
            get { return Interlocked.Read(ref _bytesSent); }
        }

        public long PacketsReceived
        {
            get { return Interlocked.Read(ref _packetsReceived); }
        }

        public long PacketsSent
        {
            get { return Interlocked.Read(ref _packetsSent); }
        }

        public void RecordReceived(int count)
        {
            Interlocked.Add(ref _bytesReceived, count);
            Interlocked.Increment(ref _packetsReceived);
        }

        public void RecordSent(int count)
        {
            Interlocked.Add(ref _bytesSent, count);
            Interlocked.Increment(ref _packetsSent);
        }

        public void Reset()
        {
            Interlocked.Exchange(ref _bytesReceived, 0);
            Interlocked.Exchange(ref _bytesSent, 0);
            Interlocked.Exchange(ref _packetsReceived, 0);
            Interlocked.Exchange(ref _packetsSent, 0);
        }
    }

    /// <summary>
    /// 把字节数格式化成 "1.5 KB" 这样的可读文本。
    /// </summary>
    public static class ByteSize
    {
        private static readonly string[] Units = new[] { "KB", "MB", "GB", "TB" };

        public static string Format(long bytes)
        {
            if (bytes < 1024)
            {
                return bytes.ToString(CultureInfo.InvariantCulture) + " B";
            }

            var value = (double)bytes;
            var unit = -1;
            do
            {
                value /= 1024.0;
                unit++;
            }
            while (value >= 1024.0 && unit < Units.Length - 1);

            return value.ToString("0.0", CultureInfo.InvariantCulture) + " " + Units[unit];
        }
    }
}
