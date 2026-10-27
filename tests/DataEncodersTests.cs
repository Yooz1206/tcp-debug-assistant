using System;
using System.Linq;
using TcpDebug.Core;

namespace TcpDebug.Core.Tests
{
    public static class DataEncodersTests
    {
        public static void Register()
        {
            TestRunner.Add("DataEncoders.Names 包含 UTF-8 与 ASCII", () =>
            {
                Assert.True(DataEncoders.Names.Contains("UTF-8"));
                Assert.True(DataEncoders.Names.Contains("ASCII"));
            });

            TestRunner.Add("DataEncoders.Get 不区分大小写", () =>
            {
                var encoding = DataEncoders.Get("utf-8");
                Assert.Equal("utf-8", encoding.WebName);
            });

            TestRunner.Add("DataEncoders.Get 的 UTF-8 可以往返中文", () =>
            {
                var bytes = DataEncoders.Get("UTF-8").GetBytes("你好，TCP");
                Assert.Equal("你好，TCP", DataEncoders.Get("UTF-8").GetString(bytes));
            });

            TestRunner.Add("DataEncoders.Get 遇到未知名称抛出 ArgumentException", () =>
                Assert.Throws<ArgumentException>(() => DataEncoders.Get("klingon-8")));

            TestRunner.Add("DataEncoders 解码非法字节时不抛异常", () =>
            {
                var text = DataEncoders.Get("ASCII").GetString(new byte[] { 0x41, 0xFF, 0x42 });
                Assert.True(text.Length > 0);
            });

            TestRunner.Add("DataEncoders.Names 中的每一项都能取到编码对象", () =>
            {
                foreach (var name in DataEncoders.Names)
                {
                    Assert.NotNull(DataEncoders.Get(name));
                }
            });
        }
    }
}
