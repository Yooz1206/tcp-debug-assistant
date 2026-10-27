using System;
using System.Text;

namespace TcpDebug.Core
{
    /// <summary>
    /// 把字节流渲染成常见的十六进制视图：每行 16 字节，右侧附带 ASCII 预览。
    /// </summary>
    public static class HexDumpFormatter
    {
        public const int BytesPerLine = 16;

        public static string Format(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder(data.Length * 4);
            for (var offset = 0; offset < data.Length; offset += BytesPerLine)
            {
                if (offset > 0)
                {
                    builder.Append('\n');
                }

                var count = Math.Min(BytesPerLine, data.Length - offset);
                for (var i = 0; i < BytesPerLine; i++)
                {
                    if (i < count)
                    {
                        builder.Append(data[offset + i].ToString("X2")).Append(' ');
                    }
                    else
                    {
                        builder.Append("   ");
                    }
                }

                builder.Append('|');
                for (var i = 0; i < BytesPerLine; i++)
                {
                    if (i < count)
                    {
                        var value = data[offset + i];
                        builder.Append(value >= 0x20 && value <= 0x7E ? (char)value : '.');
                    }
                    else
                    {
                        builder.Append(' ');
                    }
                }

                builder.Append('|');
            }

            return builder.ToString();
        }
    }
}
