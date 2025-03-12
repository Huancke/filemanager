@echo off
echo 正在启动文件管理器...
cd /d D:\Code\FileManager\bin\Debug\net8.0-windows
echo 当前目录: %CD%
echo 检查文件是否存在...
if exist FileManager.exe (
    echo 文件存在，正在启动...
    powershell -Command "Start-Process 'FileManager.exe' -Verb RunAs"
    echo 启动命令已执行
) else (
    echo 错误: FileManager.exe 不存在!
)
echo 文件管理器已启动，请查看任务栏或桌面。
pause 