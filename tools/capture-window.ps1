param(
    [string]$ProcessName = "TcpDebugAssistant",
    [string]$Output = "window.png"
)

# 让 PowerShell 进程按 DPI 感知运行，否则在 125% / 150% 缩放下
# GetWindowRect 返回的是虚拟像素，截出来的图会被裁掉一块。
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public class DpiHelper {
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);
}
"@
[DpiHelper]::SetProcessDPIAware() | Out-Null
Add-Type -AssemblyName System.Drawing

$process = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $process) {
    throw "进程 $ProcessName 没有运行"
}

$process.Refresh()
$handle = $process.MainWindowHandle
if ($handle -eq [IntPtr]::Zero) {
    throw "进程 $ProcessName 还没有主窗口"
}

$rect = New-Object DpiHelper+RECT
[DpiHelper]::GetWindowRect($handle, [ref]$rect) | Out-Null
$width = $rect.Right - $rect.Left
$height = $rect.Bottom - $rect.Top

$bitmap = New-Object System.Drawing.Bitmap $width, $height
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$hdc = $graphics.GetHdc()
$ok = [DpiHelper]::PrintWindow($handle, $hdc, 2)
$graphics.ReleaseHdc($hdc)
$graphics.Dispose()

$fullPath = [System.IO.Path]::GetFullPath($Output)
$bitmap.Save($fullPath, [System.Drawing.Imaging.ImageFormat]::Png)
$bitmap.Dispose()

"captured ${width}x${height} -> $fullPath (printWindow=$ok)"
