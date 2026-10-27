using System;
using System.Collections.Generic;
using System.Linq;

namespace TcpDebug.Core.Tests
{
    public sealed class AssertionException : Exception
    {
        public AssertionException(string message) : base(message)
        {
        }
    }

    public static class Assert
    {
        public static void True(bool condition, string message = null)
        {
            if (!condition)
            {
                throw new AssertionException(message ?? "期望为 true，实际为 false");
            }
        }

        public static void False(bool condition, string message = null)
        {
            if (condition)
            {
                throw new AssertionException(message ?? "期望为 false，实际为 true");
            }
        }

        public static void Equal<T>(T expected, T actual, string message = null)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new AssertionException(
                    string.Format("期望 <{0}>{1}，实际 <{2}>", Format(expected), Suffix(message), Format(actual)));
            }
        }

        public static void NotEqual<T>(T unexpected, T actual, string message = null)
        {
            if (EqualityComparer<T>.Default.Equals(unexpected, actual))
            {
                throw new AssertionException(string.Format("不应等于 <{0}>{1}", Format(actual), Suffix(message)));
            }
        }

        public static void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, string message = null)
        {
            var e = expected.ToArray();
            var a = actual.ToArray();
            if (!e.SequenceEqual(a))
            {
                throw new AssertionException(string.Format("期望 [{0}]{1}，实际 [{2}]",
                    string.Join(", ", e.Select(x => Format(x))), Suffix(message), string.Join(", ", a.Select(x => Format(x)))));
            }
        }

        public static void Null(object value, string message = null)
        {
            if (value != null)
            {
                throw new AssertionException(string.Format("期望为 null{0}，实际 <{1}>", Suffix(message), Format(value)));
            }
        }

        public static void NotNull(object value, string message = null)
        {
            if (value == null)
            {
                throw new AssertionException("期望非 null" + Suffix(message) + "，实际为 null");
            }
        }

        public static void Contains(string expectedSubstring, string actual, string message = null)
        {
            if (actual == null || actual.IndexOf(expectedSubstring, StringComparison.Ordinal) < 0)
            {
                throw new AssertionException(string.Format("期望包含 <{0}>{1}，实际 <{2}>",
                    expectedSubstring, Suffix(message), Format(actual)));
            }
        }

        public static TException Throws<TException>(Action action, string message = null) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }
            catch (Exception ex)
            {
                throw new AssertionException(string.Format("期望抛出 {0}{1}，实际抛出 {2}: {3}",
                    typeof(TException).Name, Suffix(message), ex.GetType().Name, ex.Message));
            }

            throw new AssertionException(string.Format("期望抛出 {0}{1}，实际没有异常", typeof(TException).Name, Suffix(message)));
        }

        public static System.Threading.Tasks.Task<TException> ThrowsAsync<TException>(Func<System.Threading.Tasks.Task> action, string message = null) where TException : Exception
        {
            var tcs = new System.Threading.Tasks.TaskCompletionSource<TException>();
            try
            {
                var task = action();
                task.ContinueWith(t =>
                {
                    if (t.IsCanceled)
                    {
                        tcs.TrySetResult(CreateMismatch<TException>("任务被取消", message));
                    }
                    else if (t.Exception != null)
                    {
                        var ex = t.Exception.GetBaseException();
                        if (ex is TException)
                        {
                            tcs.TrySetResult((TException)ex);
                        }
                        else
                        {
                            tcs.TrySetResult(CreateMismatch<TException>(
                                string.Format("{0}: {1}", ex.GetType().Name, ex.Message), message));
                        }
                    }
                    else
                    {
                        tcs.TrySetResult(CreateMismatch<TException>("没有抛出异常", message));
                    }
                });
            }
            catch (Exception ex)
            {
                if (ex is TException)
                {
                    tcs.TrySetResult((TException)ex);
                }
                else
                {
                    tcs.TrySetResult(CreateMismatch<TException>(
                        string.Format("{0}: {1}", ex.GetType().Name, ex.Message), message));
                }
            }

            return tcs.Task;
        }

        private static TException CreateMismatch<TException>(string detail, string message) where TException : Exception
        {
            return (TException)Activator.CreateInstance(typeof(TException),
                string.Format("期望抛出 {0}{1}，实际{2}", typeof(TException).Name, Suffix(message), detail));
        }

        private static string Suffix(string message)
        {
            return string.IsNullOrEmpty(message) ? string.Empty : "（" + message + "）";
        }

        private static string Format(object value)
        {
            if (value == null)
            {
                return "null";
            }

            var bytes = value as byte[];
            if (bytes != null)
            {
                return "[" + string.Join(" ", bytes.Select(b => b.ToString("X2"))) + "]";
            }

            var text = value as string;
            if (text != null)
            {
                return "\"" + text.Replace("\r", "\\r").Replace("\n", "\\n").Replace(" ", "·") + "\"";
            }

            return value.ToString() ?? "null";
        }
    }
}
