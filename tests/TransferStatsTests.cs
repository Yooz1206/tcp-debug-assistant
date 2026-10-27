using TcpDebug.Core;

namespace TcpDebug.Core.Tests
{
    public static class TransferStatsTests
    {
        public static void Register()
        {
            TestRunner.Add("TransferStats 累计收发的字节和包数", () =>
            {
                var stats = new TransferStats();
                stats.RecordReceived(100);
                stats.RecordReceived(23);
                stats.RecordSent(7);

                Assert.Equal(123L, stats.BytesReceived);
                Assert.Equal(2L, stats.PacketsReceived);
                Assert.Equal(7L, stats.BytesSent);
                Assert.Equal(1L, stats.PacketsSent);
            });

            TestRunner.Add("TransferStats.Reset 清零所有计数", () =>
            {
                var stats = new TransferStats();
                stats.RecordReceived(10);
                stats.RecordSent(10);
                stats.Reset();

                Assert.Equal(0L, stats.BytesReceived);
                Assert.Equal(0L, stats.BytesSent);
                Assert.Equal(0L, stats.PacketsReceived);
                Assert.Equal(0L, stats.PacketsSent);
            });

            TestRunner.Add("ByteSize.Format 以友好单位显示", () =>
            {
                Assert.Equal("0 B", ByteSize.Format(0));
                Assert.Equal("999 B", ByteSize.Format(999));
                Assert.Equal("1.0 KB", ByteSize.Format(1024));
                Assert.Equal("1.5 KB", ByteSize.Format(1536));
                Assert.Equal("1.0 MB", ByteSize.Format(1024 * 1024));
                Assert.Equal("2.5 GB", ByteSize.Format((long)(2.5 * 1024 * 1024 * 1024)));
            });
        }
    }
}
