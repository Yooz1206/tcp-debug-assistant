<#
    TCP 调试助手 · 构建脚本

    不需要安装 .NET SDK：直接使用 Windows 自带的 C# 编译器（.NET Framework 4.8），
    产物是一个免安装的单文件 exe。

    用法：
        .\build.ps1              构建 dist\TcpDebugAssistant.exe
        .\build.ps1 -Test        构建并运行测试
        .\build.ps1 -Test -Filter Hex   只运行名称包含 Hex 的测试
        .\build.ps1 -Run         构建并启动程序
#>
[CmdletBinding()]
param(
    [switch]$Test,
    [switch]$Run,
    [string]$Filter
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Force -Path $dist | Out-Null

function Get-CscPath {
    $candidates = @(
        (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'),
        (Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe')
    )
    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) { return $candidate }
    }

    throw '找不到 .NET Framework 自带的 C# 编译器 csc.exe，请确认系统为 Windows 10/11。'
}

$csc = Get-CscPath
$refs = @('/r:System.dll', '/r:System.Core.dll', '/r:System.Drawing.dll', '/r:System.Windows.Forms.dll')
$common = @('/nologo', '/platform:anycpu', '/codepage:65001', '/utf8output', '/warn:4')

function Get-Sources([string]$relativePath) {
    return @(Get-ChildItem (Join-Path $root $relativePath) -Filter *.cs -Recurse |
        Sort-Object FullName |
        ForEach-Object { $_.FullName })
}

function Invoke-Csc([string[]]$arguments, [string]$failureMessage) {
    # 合并 stderr：否则 $ErrorActionPreference='Stop' 会把编译错误当成脚本异常直接中断。
    $output = & $csc $arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        foreach ($line in $output) { Write-Host ([string]$line) -ForegroundColor Red }
        throw $failureMessage
    }

    return $output
}

$coreSources = Get-Sources 'src\Core'

function Build-Tests {
    $out = Join-Path $dist 'TcpDebug.Tests.exe'
    $cscArgs = $common + @('/target:exe', "/out:$out") + $refs + $coreSources + (Get-Sources 'tests')
    Invoke-Csc $cscArgs '测试编译失败' | Out-Null
    Write-Host "已编译测试: $out" -ForegroundColor DarkGray
    return $out
}

function Build-App {
    $out = Join-Path $dist 'TcpDebugAssistant.exe'
    $cscArgs = $common + @('/target:winexe', '/optimize+', "/out:$out") + $refs + $coreSources + (Get-Sources 'src\App')

    $manifest = Join-Path $root 'src\App\app.manifest'
    if (Test-Path $manifest) { $cscArgs += "/win32manifest:$manifest" }

    $icon = Join-Path $root 'src\App\app.ico'
    if (Test-Path $icon) { $cscArgs += "/win32icon:$icon" }

    Invoke-Csc $cscArgs '程序编译失败' | Out-Null

    $size = [Math]::Round((Get-Item $out).Length / 1KB, 1)
    Write-Host "已生成: $out ($size KB)" -ForegroundColor Green
    return $out
}

if ($Test) {
    $testsExe = Build-Tests
    $testArgs = @()
    if ($Filter) { $testArgs += $Filter }
    & $testsExe $testArgs
    exit $LASTEXITCODE
}

$app = Build-App
if ($Run) {
    Start-Process -FilePath $app | Out-Null
}
