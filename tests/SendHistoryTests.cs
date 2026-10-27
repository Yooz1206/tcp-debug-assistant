using TcpDebug.Core;

namespace TcpDebug.Core.Tests
{
    public static class SendHistoryTests
    {
        public static void Register()
        {
            TestRunner.Add("SendHistory 倒序回看发送记录", () =>
            {
                var history = new SendHistory();
                history.Add("first");
                history.Add("second");
                history.Add("third");

                Assert.Equal("third", history.Previous());
                Assert.Equal("second", history.Previous());
                Assert.Equal("first", history.Previous());
                Assert.Null(history.Previous());
            });

            TestRunner.Add("SendHistory 回看后可以向后翻", () =>
            {
                var history = new SendHistory();
                history.Add("a");
                history.Add("b");
                history.Previous();
                history.Previous();

                Assert.Equal("b", history.Next());
                Assert.Null(history.Next());
            });

            TestRunner.Add("SendHistory 忽略空白输入", () =>
            {
                var history = new SendHistory();
                history.Add("  ");
                Assert.Equal(0, history.Items.Count);
            });

            TestRunner.Add("SendHistory 忽略与上一条重复的记录", () =>
            {
                var history = new SendHistory();
                history.Add("same");
                history.Add("same");
                Assert.Equal(1, history.Items.Count);
            });

            TestRunner.Add("SendHistory 超出容量时丢弃最旧的记录", () =>
            {
                var history = new SendHistory(2);
                history.Add("1");
                history.Add("2");
                history.Add("3");
                Assert.SequenceEqual(new[] { "2", "3" }, history.Items);
            });

            TestRunner.Add("SendHistory 新增记录后回看从头开始", () =>
            {
                var history = new SendHistory();
                history.Add("old");
                history.Previous();
                history.Add("new");
                Assert.Equal("new", history.Previous());
            });
        }
    }
}
