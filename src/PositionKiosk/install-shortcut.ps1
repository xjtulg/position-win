# 在当前用户的"启动"文件夹创建 PositionKiosk 开机自启快捷方式
# 用法：在发布产物目录（含 PositionKiosk.exe）下运行 powershell -ExecutionPolicy Bypass -File .\install-shortcut.ps1
$ErrorActionPreference = 'Stop'

$exe = Join-Path $PSScriptRoot 'PositionKiosk.exe'
if (-not (Test-Path $exe)) {
    throw "未找到 $exe，请在发布产物目录运行此脚本。"
}

$startup = [Environment]::GetFolderPath('Startup')
$shortcutPath = Join-Path $startup 'PositionKiosk.lnk'

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $exe
$shortcut.WorkingDirectory = $PSScriptRoot
$shortcut.WindowStyle = 3   # 最大化
$shortcut.Description = 'Position Kiosk'
$shortcut.Save()

Write-Host "已创建开机自启快捷方式：$shortcutPath"
