using System;
using TcpDebug.Core;

namespace TcpDebug.Core.Tests
{
    public static class HexCodecTests
    {
        public static void Register()
        {
            TestRunner.Add("HexCodec.Parse 解析空格分隔的十六进制", () =>
                Assert.SequenceEqual(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }, HexCodec.Parse("48 65 6C 6C 6F")));

            TestRunner.Add("HexCodec.Parse 解析连续的十六进制", () =>
                Assert.SequenceEqual(new byte[] { 0x48, 0x65, 0x6C }, HexCodec.Parse("48656C")));

            TestRunner.Add("HexCodec.Parse 支持 0x 前缀和逗号分隔", () =>
                Assert.SequenceEqual(new byte[] { 0x00, 0xFF, 0x0A }, HexCodec.Parse("0x00, 0xFF, 0x0A")));

            TestRunner.Add("HexCodec.Parse 忽略换行、制表符和短横线", () =>
                Assert.SequenceEqual(new byte[] { 0x01, 0x02, 0x03, 0x04 },
                    HexCodec.Parse("01\r\n02\t03-04")));

            TestRunner.Add("HexCodec.Parse 解析单字节", () =>
                Assert.SequenceEqual(new byte[] { 0x5A }, HexCodec.Parse("5a")));

            TestRunner.Add("HexCodec.Parse 空白输入返回空数组", () =>
                Assert.Equal(0, HexCodec.Parse("   \r\n ").Length));

            TestRunner.Add("HexCodec.Parse 字符数为奇数时抛出 FormatException", () =>
                Assert.Throws<FormatException>(() => HexCodec.Parse("ABC")));

            TestRunner.Add("HexCodec.Parse 含非法字符时抛出 FormatException", () =>
                Assert.Throws<FormatException>(() => HexCodec.Parse("48 ZZ")));

            TestRunner.Add("HexCodec.Parse 的异常信息指明出错位置", () =>
            {
                var ex = Assert.Throws<FormatException>(() => HexCodec.Parse("48 ZZ"));
                Assert.Contains("Z", ex.Message);
            });

            TestRunner.Add("HexCodec.TryParse 失败时返回 false 和错误说明", () =>
            {
                byte[] bytes;
                string error;
                var ok = HexCodec.TryParse("48 ZZ", out bytes, out error);
                Assert.False(ok);
                Assert.Equal(0, bytes.Length);
                Assert.NotNull(error);
            });

            TestRunner.Add("HexCodec.TryParse 成功时返回 true", () =>
            {
                byte[] bytes;
                string error;
                var ok = HexCodec.TryParse("0A 0B", out bytes, out error);
                Assert.True(ok);
                Assert.Null(error);
                Assert.SequenceEqual(new byte[] { 0x0A, 0x0B }, bytes);
            });

            TestRunner.Add("HexCodec.ToHex 输出大写并用空格分隔", () =>
                Assert.Equal("48 65 0A", HexCodec.ToHex(new byte[] { 0x48, 0x65, 0x0A })));

            TestRunner.Add("HexCodec.ToHex 空数组返回空字符串", () =>
                Assert.Equal(string.Empty, HexCodec.ToHex(new byte[0])));
        }
    }
}
