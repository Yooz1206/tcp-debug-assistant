using System;
using System.Collections.Generic;
using System.Text;

namespace TcpDebug.Core
{
    /// <summary>
    /// 十六进制文本与字节数组之间的转换。
    /// 输入容忍空格、逗号、分号、短横线、换行、0x 前缀等常见写法。
    /// </summary>
    public static class HexCodec
    {
        private static readonly char[] Separators = new[] { ' ', '\t', '\r', '\n', ',', ';', '-', '|', ':' };

        public static byte[] Parse(string text)
        {
            byte[] bytes;
            string error;
            if (!TryParse(text, out bytes, out error))
            {
                throw new FormatException(error);
            }

            return bytes;
        }

        public static bool TryParse(string text, out byte[] bytes, out string error)
        {
            bytes = new byte[0];
            error = null;

            if (text == null)
            {
                error = "十六进制内容为空。";
                return false;
            }

            var digits = new StringBuilder(text.Length);
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                // 跳过 0x / 0X 前缀
                if ((c == '0') && (i + 1 < text.Length) && (text[i + 1] == 'x' || text[i + 1] == 'X'))
                {
                    i++;
                    continue;
                }

                if (Array.IndexOf(Separators, c) >= 0)
                {
                    continue;
                }

                if (!IsHexDigit(c))
                {
                    error = string.Format("第 {0} 个字符 '{1}' 不是合法的十六进制字符。", i + 1, c);
                    return false;
                }

                digits.Append(c);
            }

            if (digits.Length % 2 != 0)
            {
                error = string.Format("十六进制字符个数为 {0}，必须成对出现（每个字节 2 个字符）。", digits.Length);
                return false;
            }

            bytes = new byte[digits.Length / 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                var high = HexValue(digits[i * 2]);
                var low = HexValue(digits[i * 2 + 1]);
                bytes[i] = (byte)((high << 4) | low);
            }

            return true;
        }

        public static string ToHex(byte[] data, string separator = " ")
        {
            if (data == null || data.Length == 0)
            {
                return string.Empty;
            }

            if (separator == null)
            {
                separator = string.Empty;
            }

            var builder = new StringBuilder(data.Length * (2 + separator.Length));
            for (var i = 0; i < data.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(separator);
                }

                builder.Append(data[i].ToString("X2"));
            }

            return builder.ToString();
        }

        private static bool IsHexDigit(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
        }

        private static int HexValue(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return c - '0';
            }

            if (c >= 'a' && c <= 'f')
            {
                return c - 'a' + 10;
            }

            return c - 'A' + 10;
        }
    }
}
