@echo off
chcp 936 >nul
cd /d "%~dp0"

rem ============================================================
rem  多人联机启动器
rem  把这个 bat 和 MyRBG-test.exe 放在同一个文件夹里，双击即可。
rem  它不依赖 server.txt：即使配置丢了也能连上服务器。
rem ============================================================

set "SERVER=124.223.10.2"
set "PORT=7777"

rem 同目录下有 server.txt 时，以里面的地址为准（第一行，形如 1.2.3.4 或 1.2.3.4:7777）
if exist "%~dp0server.txt" set /p SERVER=<"%~dp0server.txt"

if not exist "%~dp0MyRBG-test.exe" goto noexe

echo.
echo  正在连接服务器：%SERVER%
echo  如果一直停在"正在连接"，请把整个文件夹截图发给我。
echo.
start "" "%~dp0MyRBG-test.exe" -join %SERVER% -port %PORT%
exit /b 0

:noexe
echo.
echo  [错误] 这个 bat 必须和 MyRBG-test.exe 放在同一个文件夹里。
echo         当前文件夹：%~dp0
echo.
pause
exit /b 1