using System;
using System.Collections.Generic;
using System.Text;

namespace TcpDebug.Core
{
    /// <summary>
    /// 可选的文本编码。解码时使用替换回退，避免收到非法字节时抛异常中断调试。
    /// </summary>
    public static class DataEncoders
    {
        private static readonly List<string> KnownNames = new List<string>();
        private static readonly Dictionary<string, Encoding> Encodings =
            new Dictionary<string, Encoding>(StringComparer.OrdinalIgnoreCase);

        static DataEncoders()
        {
            Add("UTF-8", 65001);
            Add("GBK", 936);
            Add("GB18030", 54936);
            Add("ASCII", 20127);
            Add("Latin-1", 28591);
            Add("UTF-16LE", 1200);
        }

        public static IReadOnlyList<string> Names
        {
            get { return KnownNames; }
        }

        public static Encoding Get(string name)
        {
            Encoding encoding;
            if (name != null && Encodings.TryGetValue(name, out encoding))
            {
                return encoding;
            }

            throw new ArgumentException(
                string.Format("不支持的编码 \"{0}\"。可用编码：{1}", name, string.Join("、", KnownNames.ToArray())), "name");
        }

        private static void Add(string name, int codePage)
        {
            try
            {
                var encoding = Encoding.GetEncoding(
                    codePage, EncoderFallback.ReplacementFallback, DecoderFallback.ReplacementFallback);
                Encodings[name] = encoding;
                KnownNames.Add(name);
            }
            catch (ArgumentException)
            {
                // 当前运行环境没有该代码页（例如精简版 Windows），直接跳过。
            }
            catch (NotSupportedException)
            {
            }
        }
    }
}
