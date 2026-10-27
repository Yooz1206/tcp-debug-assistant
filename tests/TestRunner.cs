using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace TcpDebug.Core.Tests
{
    public sealed class TestCase
    {
        public TestCase(string name, Func<Task> body)
        {
            Name = name;
            Body = body;
        }

        public string Name { get; private set; }
        public Func<Task> Body { get; private set; }
    }

    /// <summary>
    /// 极简测试运行器：不依赖 NuGet，构建和运行都不需要联网。
    /// </summary>
    public static class TestRunner
    {
        private static readonly List<TestCase> Cases = new List<TestCase>();

        public static void Add(string name, Func<Task> body)
        {
            Cases.Add(new TestCase(name, body));
        }

        public static void Add(string name, Action body)
        {
            Add(name, () =>
            {
                body();
                return Task.FromResult(0);
            });
        }

        public static int Run(string filter)
        {
            var selected = Cases
                .Where(c => string.IsNullOrEmpty(filter) ||
                            c.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            if (selected.Count == 0)
            {
                Console.WriteLine("没有任何用例匹配过滤器 " + filter);
                return 2;
            }

            Console.WriteLine("TCP 调试助手 · Core 测试，共 " + selected.Count + " 个用例");
            Console.WriteLine(new string('-', 70));

            var failed = 0;
            var total = Stopwatch.StartNew();
            foreach (var test in selected)
            {
                var caseWatch = Stopwatch.StartNew();
                try
                {
                    test.Body().GetAwaiter().GetResult();
                    Console.WriteLine("  PASS  " + test.Name + "  (" + caseWatch.ElapsedMilliseconds + " ms)");
                }
                catch (Exception ex)
                {
                    failed++;
                    Console.WriteLine("  FAIL  " + test.Name + "  (" + caseWatch.ElapsedMilliseconds + " ms)");
                    Console.WriteLine("        " + ex.Message.Replace("\n", "\n        "));
                    if (!(ex is AssertionException))
                    {
                        Console.WriteLine("        " + ex.GetType().FullName);
                        Console.WriteLine("        " + (ex.StackTrace ?? string.Empty).Replace("\n", "\n        "));
                    }
                }
            }

            Console.WriteLine(new string('-', 70));
            Console.WriteLine(failed == 0
                ? string.Format("全部通过：{0} 个用例，用时 {1} ms", selected.Count, total.ElapsedMilliseconds)
                : string.Format("失败 {0} 个，通过 {1} 个，用时 {2} ms", failed, selected.Count - failed, total.ElapsedMilliseconds));

            return failed == 0 ? 0 : 1;
        }
    }
}
