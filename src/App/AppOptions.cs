using System;
using System.Globalization;

namespace TcpDebug.App
{
    /// <summary>
    /// 启动参数，例如：
    ///   TcpDebugAssistant.exe --server --port 9100
    ///   TcpDebugAssistant.exe --host 192.168.1.10 --port 5000 --connect
    /// </summary>
    public sealed class AppOptions
    {
        public AppOptions()
        {
        }

        public bool ServerMode { get; set; }

        public string Host { get; set; }

        public int? Port { get; set; }

        public bool AutoConnect { get; set; }

        public static AppOptions Parse(string[] args)
        {
            var options = new AppOptions();
            if (args == null)
            {
                return options;
            }

            for (var i = 0; i < args.Length; i++)
            {
                var argument = args[i] ?? string.Empty;
                switch (argument.ToLowerInvariant())
                {
                    case "--server":
                        options.ServerMode = true;
                        break;

                    case "--client":
                        options.ServerMode = false;
                        break;

                    case "--connect":
                        options.AutoConnect = true;
                        break;

                    case "--host":
                        if (i + 1 < args.Length)
                        {
                            options.Host = args[++i];
                        }

                        break;

                    case "--port":
                        if (i + 1 < args.Length)
                        {
                            int port;
                            if (int.TryParse(args[++i], NumberStyles.Integer, CultureInfo.InvariantCulture, out port))
                            {
                                options.Port = port;
                            }
                        }

                        break;
                }
            }

            return options;
        }
    }
}
