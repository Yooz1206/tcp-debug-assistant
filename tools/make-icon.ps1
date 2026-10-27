param(
    [string]$Output = "src\App\app.ico"
)

# 生成应用图标：多尺寸 ICO（内嵌 PNG），与界面里的 LogoMark 保持一致。
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Drawing.Common -ErrorAction SilentlyContinue
$ErrorActionPreference = 'Stop'

function New-IconBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    $radius = [Math]::Max(2.0, $size * 0.28)
    $rect = New-Object System.Drawing.RectangleF 0.5, 0.5, ($size - 1), ($size - 1)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $radius * 2
    $path.AddArc($rect.X, $rect.Y, $d, $d, 180, 90)
    $path.AddArc($rect.Right - $d, $rect.Y, $d, $d, 270, 90)
    $path.AddArc($rect.Right - $d, $rect.Bottom - $d, $d, $d, 0, 90)
    $path.AddArc($rect.X, $rect.Bottom - $d, $d, $d, 90, 90)
    $path.CloseFigure()

    $from = [System.Drawing.ColorTranslator]::FromHtml("#5C9CFF")
    $to = [System.Drawing.ColorTranslator]::FromHtml("#2B5BD7")
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, $from, $to, 55.0)
    $g.FillPath($brush, $path)

    $white = [System.Drawing.Color]::White
    $penWidth = [Math]::Max(1.0, $size * 0.085)
    $pen = New-Object System.Drawing.Pen $white, $penWidth
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round

    $s = $size / 26.0
    $g.DrawLine($pen, 7 * $s, 10 * $s, 17 * $s, 10 * $s)
    $g.DrawLine($pen, 9 * $s, 17 * $s, 19 * $s, 17 * $s)

    $head = New-Object System.Drawing.SolidBrush $white
    $g.FillPolygon($head, @(
        (New-Object System.Drawing.PointF (16.5 * $s), (7 * $s)),
        (New-Object System.Drawing.PointF (21.5 * $s), (10 * $s)),
        (New-Object System.Drawing.PointF (16.5 * $s), (13 * $s))
    ))
    $g.FillPolygon($head, @(
        (New-Object System.Drawing.PointF (9.5 * $s), (14 * $s)),
        (New-Object System.Drawing.PointF (4.5 * $s), (17 * $s)),
        (New-Object System.Drawing.PointF (9.5 * $s), (20 * $s))
    ))

    $pen.Dispose()
    $brush.Dispose()
    $head.Dispose()
    $path.Dispose()
    $g.Dispose()
    return $bmp
}

$sizes = @(16, 24, 32, 48, 64, 128, 256)
$images = @()
foreach ($size in $sizes) {
    $bmp = New-IconBitmap $size
    $stream = New-Object System.IO.MemoryStream
    $bmp.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $images += , @{ Size = $size; Data = $stream.ToArray() }
    $bmp.Dispose()
    $stream.Dispose()
}

$fullPath = [System.IO.Path]::GetFullPath($Output)
$directory = [System.IO.Path]::GetDirectoryName($fullPath)
if (-not (Test-Path $directory)) { New-Item -ItemType Directory -Force -Path $directory | Out-Null }

$fs = [System.IO.File]::Create($fullPath)
$writer = New-Object System.IO.BinaryWriter $fs

# ICONDIR
$writer.Write([UInt16]0)
$writer.Write([UInt16]1)
$writer.Write([UInt16]$images.Count)

# 先算好每张图的数据偏移
$offset = 6 + (16 * $images.Count)
foreach ($image in $images) {
    $size = [int]$image.Size
    $writer.Write([Byte]$(if ($size -ge 256) { 0 } else { $size }))
    $writer.Write([Byte]$(if ($size -ge 256) { 0 } else { $size }))
    $writer.Write([Byte]0)
    $writer.Write([Byte]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]32)
    $writer.Write([UInt32]$image.Data.Length)
    $writer.Write([UInt32]$offset)
    $offset += $image.Data.Length
}

foreach ($image in $images) {
    $writer.Write($image.Data)
}

$writer.Flush()
$writer.Dispose()
$fs.Dispose()

"icon written: $fullPath ($((Get-Item $fullPath).Length) bytes, $($images.Count) sizes)"
