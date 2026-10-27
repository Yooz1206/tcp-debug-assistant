using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace TcpDebug.App
{
    /// <summary>
    /// 使用简单的 key=value 文本保存界面设置，避免引入额外依赖。
    /// </summary>
    internal sealed class AppSettings
    {
        public AppSettings()
        {
            Host = "127.0.0.1";
            Port = 8080;
            EncodingName = "UTF-8";
            AutoSendInterval = 1000;
            ShowTimestamps = true;
            Width = 1200;
            Height = 800;
        }

        public bool ServerMode { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        public string EncodingName { get; set; }

        public bool HexSend { get; set; }

        public bool AppendCrlf { get; set; }

        public bool AutoSend { get; set; }

        public int AutoSendInterval { get; set; }

        public bool ShowTimestamps { get; set; }

        public bool HexView { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public bool Maximized { get; set; }

        public static string FilePath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TcpDebugAssistant",
                    "settings.ini");
            }
        }

        public static AppSettings Load()
        {
            var settings = new AppSettings();
            try
            {
                if (!File.Exists(FilePath))
                {
                    return settings;
                }

                foreach (var line in File.ReadAllLines(FilePath, Encoding.UTF8))
                {
                    var separator = line.IndexOf('=');
                    if (separator <= 0)
                    {
                        continue;
                    }

                    var key = line.Substring(0, separator).Trim();
                    var value = line.Substring(separator + 1).Trim();
                    settings.Apply(key, value);
                }
            }
            catch (Exception)
            {
                // 设置文件损坏时退回默认值，不影响启动。
            }

            return settings;
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var lines = new List<string>
                {
                    "ServerMode=" + Bool(ServerMode),
                    "Host=" + Host,
                    "Port=" + Port.ToString(CultureInfo.InvariantCulture),
                    "Encoding=" + EncodingName,
                    "HexSend=" + Bool(HexSend),
                    "AppendCrlf=" + Bool(AppendCrlf),
                    "AutoSend=" + Bool(AutoSend),
                    "AutoSendInterval=" + AutoSendInterval.ToString(CultureInfo.InvariantCulture),
                    "ShowTimestamps=" + Bool(ShowTimestamps),
                    "HexView=" + Bool(HexView),
                    "Width=" + Width.ToString(CultureInfo.InvariantCulture),
                    "Height=" + Height.ToString(CultureInfo.InvariantCulture),
                    "Maximized=" + Bool(Maximized)
                };

                File.WriteAllLines(FilePath, lines.ToArray(), new UTF8Encoding(false));
            }
            catch (Exception)
            {
                // 保存失败（例如目录只读）时忽略，不影响正在进行的调试。
            }
        }

        private void Apply(string key, string value)
        {
            int number;
            switch (key.ToLowerInvariant())
            {
                case "servermode":
                    ServerMode = ParseBool(value);
                    break;
                case "host":
                    if (!string.IsNullOrEmpty(value))
                    {
                        Host = value;
                    }

                    break;
                case "port":
                    if (int.TryParse(value, out number) && number >= 0 && number <= 65535)
                    {
                        Port = number;
                    }

                    break;
                case "encoding":
                    if (!string.IsNullOrEmpty(value))
                    {
                        EncodingName = value;
                    }

                    break;
                case "hexsend":
                    HexSend = ParseBool(value);
                    break;
                case "appendcrlf":
                    AppendCrlf = ParseBool(value);
                    break;
                case "autosend":
                    AutoSend = ParseBool(value);
                    break;
                case "autosendinterval":
                    if (int.TryParse(value, out number) && number >= 50)
                    {
                        AutoSendInterval = number;
                    }

                    break;
                case "showtimestamps":
                    ShowTimestamps = ParseBool(value);
                    break;
                case "hexview":
                    HexView = ParseBool(value);
                    break;
                case "width":
                    if (int.TryParse(value, out number) && number >= 800)
                    {
                        Width = number;
                    }

                    break;
                case "height":
                    if (int.TryParse(value, out number) && number >= 600)
                    {
                        Height = number;
                    }

                    break;
                case "maximized":
                    Maximized = ParseBool(value);
                    break;
            }
        }

        private static string Bool(bool value)
        {
            return value ? "1" : "0";
        }

        private static bool ParseBool(string value)
        {
            return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
