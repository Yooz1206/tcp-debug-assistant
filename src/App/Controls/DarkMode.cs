using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TcpDebug.App
{
    /// <summary>
    /// Windows 深色外观相关的小工具：深色标题栏、深色滚动条。
    /// </summary>
    internal static class DarkMode
    {
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string subAppName, string subIdList);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        public static void ApplyTitleBar(Form form)
        {
            if (form == null || form.Handle == IntPtr.Zero)
            {
                return;
            }

            var enabled = 1;
            // 20 = DWMWA_USE_IMMERSIVE_DARK_MODE（Windows 10 2004+），19 = 更早的预览版编号。
            if (DwmSetWindowAttribute(form.Handle, 20, ref enabled, sizeof(int)) != 0)
            {
                DwmSetWindowAttribute(form.Handle, 19, ref enabled, sizeof(int));
            }
        }

        public static void ApplyControl(Control control)
        {
            if (control == null || control.IsDisposed || control.Handle == IntPtr.Zero)
            {
                return;
            }

            try
            {
                SetWindowTheme(control.Handle, "DarkMode_Explorer", null);
            }
            catch (Exception)
            {
            }
        }
    }
}
