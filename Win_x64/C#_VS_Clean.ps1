param(
    [string] $ProjectRoot
)

$ErrorActionPreference = 'Stop'
# 双击运行时，出错后保留窗口以便查看错误信息。
trap {
    Write-Host ''
    Write-Host "[ERROR] $($_.Exception.Message)" -ForegroundColor Red
    Write-Host 'Press Enter to exit...'
    Read-Host | Out-Null
    exit 1
}

# 目录自动识别：脚本所在目录名为 scripts 时，取上一级作为项目根目录；否则用脚本所在目录。
$scriptDir = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $dirName = Split-Path -Path $scriptDir -Leaf
    if ($dirName -ieq 'scripts') {
        $ProjectRoot = Split-Path -Path $scriptDir -Parent
    }
    else {
        $ProjectRoot = $scriptDir
    }
}
$ProjectRoot = $ProjectRoot.Trim().Trim('"')
$script:Root = (Resolve-Path -LiteralPath $ProjectRoot).Path.TrimEnd('\')
Set-Location -LiteralPath $script:Root
Add-Type -AssemblyName Microsoft.VisualBasic

# 校验 Visual Studio / .NET 项目特征：根目录下存在 .sln 或 .slnx 解决方案文件。
$solution = Get-ChildItem -LiteralPath $script:Root -Force -File -ErrorAction SilentlyContinue | Where-Object { $_.Extension -in '.slnx', '.sln' } | Select-Object -First 1
if (-not $solution) {
    Write-Host '未检测到 Visual Studio / .NET 项目特征（缺少 .sln 或 .slnx 解决方案文件）…跳过清理' -ForegroundColor Yellow
    for ($i = 5; $i -gt 0; $i--) {
        Write-Host "`r$i 秒后自动退出…" -NoNewline -ForegroundColor Yellow
        Start-Sleep -Seconds 1
    }
    Write-Host ''
    exit 0
}

# 将 VS/MSBuild 生成物移入回收站，保留源码和工程文件。
function Move-ToRecycleBin {
    param([Parameter(Mandatory = $true)][string] $Path)
    if (-not (Test-Path -LiteralPath $Path)) { Write-Host "[SKIP] $Path"; return }
    $item = Get-Item -LiteralPath $Path -Force
    Write-Host "[RECYCLE] $($item.FullName)"
    if ($item.PSIsContainer) {
        [Microsoft.VisualBasic.FileIO.FileSystem]::DeleteDirectory($item.FullName, [Microsoft.VisualBasic.FileIO.UIOption]::OnlyErrorDialogs, [Microsoft.VisualBasic.FileIO.RecycleOption]::SendToRecycleBin)
        return
    }
    if (($item.Attributes -band [System.IO.FileAttributes]::ReadOnly) -ne 0) { $item.Attributes = $item.Attributes -band (-bnot [System.IO.FileAttributes]::ReadOnly) }
    [Microsoft.VisualBasic.FileIO.FileSystem]::DeleteFile($item.FullName, [Microsoft.VisualBasic.FileIO.UIOption]::OnlyErrorDialogs, [Microsoft.VisualBasic.FileIO.RecycleOption]::SendToRecycleBin)
}

Write-Host "Cleaning Visual Studio and MSBuild generated files under: $script:Root"
$cleaned = 0
foreach ($path in @('.vs', '.dotnet')) {
    $target = Join-Path $script:Root $path
    if (Test-Path -LiteralPath $target) {
        Move-ToRecycleBin -Path $target
        $cleaned++
    }
}

$names = @('bin', 'obj', 'TestResults')
$dirs = Get-ChildItem -LiteralPath $script:Root -Directory -Recurse -Force -ErrorAction SilentlyContinue |
    Where-Object { $names -contains $_.Name } |
    Sort-Object { $_.FullName.Length } -Descending
foreach ($dir in $dirs) {
    Move-ToRecycleBin -Path $dir.FullName
    $cleaned++
}

if ($cleaned -eq 0) {
    Write-Host 'No Visual Studio / MSBuild generated files found.'
}
else {
    Write-Host "Done. ($cleaned item(s) recycled)"
}
