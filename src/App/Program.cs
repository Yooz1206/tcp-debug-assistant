using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace TcpDebug.App
{
    internal static class Program
    {
        private static readonly string ErrorLogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TcpDebugAssistant",
            "error.log");

        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += OnThreadException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainException;

            var options = AppOptions.Parse(args);

            try
            {
                Application.Run(new MainForm(options));
            }
            catch (Exception ex)
            {
                Report(ex);
            }
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Report(e.Exception);
        }

        private static void OnDomainException(object sender, UnhandledExceptionEventArgs e)
        {
            Report(e.ExceptionObject as Exception);
        }

        private static void Report(Exception exception)
        {
            if (exception == null)
            {
                return;
            }

            var details = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine +
                          exception + Environment.NewLine + Environment.NewLine;

            var fallbackPath = Path.Combine(Path.GetTempPath(), "TcpDebugAssistant-error.log");
            var paths = new[] { ErrorLogPath, fallbackPath, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log") };
            foreach (var path in paths)
            {
                try
                {
                    var directory = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.AppendAllText(path, details, new UTF8Encoding(false));
                    break;
                }
                catch (Exception)
                {
                }
            }

            MessageBox.Show(
                "程序遇到未处理的错误：" + Environment.NewLine +
                exception.GetType().Name + ": " + exception.Message +
                Environment.NewLine + Environment.NewLine + "详细信息已写入：" + Environment.NewLine + fallbackPath,
                "TCP 调试助手",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
