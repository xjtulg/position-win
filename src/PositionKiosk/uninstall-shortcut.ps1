# 删除当前用户的"启动"文件夹中的 PositionKiosk 开机自启快捷方式
# 用法：在发布产物目录下运行 powershell -ExecutionPolicy Bypass -File .\uninstall-shortcut.ps1
$ErrorActionPreference = 'Stop'

$startup = [Environment]::GetFolderPath('Startup')
$shortcutPath = Join-Path $startup 'PositionKiosk.lnk'

if (Test-Path $shortcutPath) {
    Remove-Item -LiteralPath $shortcutPath -Force
    Write-Host "已删除开机自启快捷方式：$shortcutPath"
} else {
    Write-Host "未找到开机自启快捷方式：$shortcutPath"
}
