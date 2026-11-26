@echo off
echo 正在导出所有C#脚本... > AllCode.txt
for /r %%i in (*.cs) do (
echo ======================================== >> AllCode.txt
echo 脚本文件路径：%%i >> AllCode.txt
echo ======================================== >> AllCode.txt
type "%%i" >> AllCode.txt
echo. >> AllCode.txt
echo. >> AllCode.txt
)
echo 导出完成，文件名为AllCode.txt
pause