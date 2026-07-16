@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

REM ============================================================
REM  Position Kiosk —— 编译打包脚本 (build.bat)
REM
REM  用法：
REM     双击运行，或命令行执行：
REM        build.bat            还原 -> 测试 -> 发布 -> 打包 zip
REM        build.bat notest     跳过单元测试，直接发布 + 打包
REM
REM  产物：
REM     .\publish\                      自包含单文件部署目录（拷到工控机即可）
REM     .\PositionKiosk-win-x64.zip     打包好的压缩包
REM ============================================================

set "ROOT=%~dp0"
set "SLN=%ROOT%PositionKiosk.sln"
set "PROJ=%ROOT%src\PositionKiosk\PositionKiosk.csproj"
set "OUT=%ROOT%publish"
set "ZIP=%ROOT%PositionKiosk-win-x64.zip"
set "CONFIG=Release"
set "RID=win-x64"

if /i "%~1"=="notest" (set "RUNTESTS=0") else (set "RUNTESTS=1")

echo ============================================================
echo   Position Kiosk 编译打包
echo   解决方案 : %SLN%
echo   输出目录 : %OUT%
echo   压缩包   : %ZIP%
echo ============================================================

REM --- 0. 环境检查 ---
where dotnet >nul 2>nul
if errorlevel 1 (
    echo [错误] 未找到 dotnet 命令，请先安装 .NET 8 SDK：
    echo        https://dotnet.microsoft.com/download
    goto :fail
)

if not exist "%SLN%" (
    echo [错误] 未找到解决方案文件 %SLN%
    echo        请先按实现计划 Task 1 创建项目脚手架：
    echo            dotnet new sln -n PositionKiosk
    echo        并建立 src\PositionKiosk\PositionKiosk.csproj。
    goto :fail
)

REM --- 1. 还原依赖 ---
echo.
echo [1/4] 还原 NuGet 依赖...
dotnet restore "%SLN%"
if errorlevel 1 goto :fail

REM --- 2. 单元测试 ---
if "%RUNTESTS%"=="1" (
    echo.
    echo [2/4] 运行单元测试 (Core^)...
    dotnet test "%SLN%" -c %CONFIG% --no-restore --nologo
    if errorlevel 1 goto :fail
) else (
    echo.
    echo [2/4] 已跳过单元测试 (notest^)
)

REM --- 3. 发布自包含单文件 ---
echo.
echo [3/4] 发布自包含单文件 (rid=%RID%, self-contained^)...
if exist "%OUT%" rmdir /s /q "%OUT%"
dotnet publish "%PROJ%" ^
  -c %CONFIG% ^
  -r %RID% ^
  --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true ^
  -o "%OUT%"
if errorlevel 1 goto :fail

REM --- 4. 打包成 zip ---
echo.
echo [4/4] 打包为 zip...
powershell -NoProfile -Command "Compress-Archive -Path '%OUT%\*' -DestinationPath '%ZIP%' -Force"
if errorlevel 1 (
    echo [警告] 打包 zip 失败，但发布目录已生成: %OUT%
) else (
    echo 已生成压缩包: %ZIP%
)

REM --- 完成 ---
echo.
echo ============================================================
echo   打包完成
echo   产物目录 : %OUT%
echo   压缩包   : %ZIP%
echo.
echo   部署步骤：
echo     1^) 把 publish 目录拷到工控机
echo     2^) 编辑 appsettings.json 修改 Url
echo     3^) 运行 PositionKiosk.exe --hash-password 设置管理员密码
echo     4^) 双击 install-shortcut.ps1 创建开机自启快捷方式
echo ============================================================
echo.
pause
exit /b 0

:fail
echo.
echo ============================================================
echo   [失败] 编译打包过程中出错，请查看上方输出。
echo ============================================================
pause
exit /b 1
