using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using TcpDebug.App;

namespace TcpDebug.UiProbe
{
    /// <summary>
    /// 开发期用的界面探针：在控制台里创建主窗体并渲染成 PNG，不随产品发布。
    /// 用法：UiProbe.exe 输出图片路径
    /// </summary>
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var output = args.Length > 0 ? args[0] : "ui.png";

            try
            {
                var form = new MainForm(new AppOptions());
                form.Show();
                Application.DoEvents();
                Console.WriteLine("窗体已创建：" + form.Width + "x" + form.Height + "  ClientSize=" + form.ClientSize);

                using (var bitmap = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height));
                    bitmap.Save(output, ImageFormat.Png);
                }

                Console.WriteLine("已渲染：" + output);
                form.Close();
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("异常：" + ex);
                return 1;
            }
        }
    }
}
