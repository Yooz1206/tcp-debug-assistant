using System;

namespace TcpDebug.Core.Tests
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            HexCodecTests.Register();
            DataEncodersTests.Register();
            HexDumpFormatterTests.Register();
            SendHistoryTests.Register();
            TransferStatsTests.Register();
            TcpLinkTests.Register();

            return TestRunner.Run(args.Length > 0 ? args[0] : null);
        }
    }
}
