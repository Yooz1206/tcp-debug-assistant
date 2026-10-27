using System;
using System.Threading.Tasks;

namespace TcpDebug.Core.Tests
{
    public static class Signals
    {
        public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

        public static TaskCompletionSource<T> New<T>()
        {
            return new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public static T Await<T>(Task<T> task)
        {
            try
            {
                if (!task.Wait(Timeout))
                {
                    throw new TimeoutException("等待异步结果超时（5 秒）");
                }
            }
            catch (AggregateException ex)
            {
                // 和 await 的行为保持一致：抛出原始异常，而不是包装后的 AggregateException。
                throw ex.InnerException;
            }

            return task.GetAwaiter().GetResult();
        }

        public static void Await(Task task)
        {
            try
            {
                if (!task.Wait(Timeout))
                {
                    throw new TimeoutException("等待异步操作超时（5 秒）");
                }
            }
            catch (AggregateException ex)
            {
                throw ex.InnerException;
            }
        }
    }
}
