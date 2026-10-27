using System;
using System.Linq;
using TcpDebug.Core;

namespace TcpDebug.Core.Tests
{
    public static class HexDumpFormatterTests
    {
        private static string Line(string hex, string ascii)
        {
            return hex.PadRight(48) + "|" + ascii.PadRight(16) + "|";
        }

        public static void Register()
        {
            TestRunner.Add("HexDumpFormatter 短包输出一行并对齐", () =>
            {
                var text = HexDumpFormatter.Format(new byte[] { 0x41, 0x42 });
                Assert.Equal(Line("41 42 ", "AB"), text);
            });

            TestRunner.Add("HexDumpFormatter 每行 16 字节", () =>
            {
                var data = Enumerable.Range(0, 17).Select(i => (byte)i).ToArray();
                var lines = HexDumpFormatter.Format(data).Split('\n');
                Assert.Equal(2, lines.Length);
                Assert.Contains("00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F", lines[0]);
                Assert.Contains("10 ", lines[1]);
            });

            TestRunner.Add("HexDumpFormatter 不可打印字节显示为点", () =>
            {
                var text = HexDumpFormatter.Format(new byte[] { 0x00, 0x1F, 0x7F, 0x20, 0x7E });
                Assert.Equal(Line("00 1F 7F 20 7E ", "... ~"), text);
            });

            TestRunner.Add("HexDumpFormatter 空数据返回空字符串", () =>
                Assert.Equal(string.Empty, HexDumpFormatter.Format(new byte[0])));

            TestRunner.Add("HexDumpFormatter 恰好 16 字节时不补多余空格", () =>
            {
                var data = Enumerable.Repeat((byte)0x30, 16).ToArray();
                var text = HexDumpFormatter.Format(data);
                Assert.Equal(Line(string.Concat(Enumerable.Repeat("30 ", 16)), "0000000000000000"), text);
            });
        }
    }
}
