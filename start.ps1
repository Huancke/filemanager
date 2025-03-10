# Start FileManager Application Script
# This script compiles the project and runs it with administrator privileges

# 检查是否以管理员权限运行
function Test-Admin {
    $currentUser = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    return $currentUser.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# 如果不是管理员权限，尝试重新以管理员权限启动
if (-not (Test-Admin)) {
    Write-Host "需要管理员权限运行此脚本..." -ForegroundColor Yellow
    
    try {
        Start-Process PowerShell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`"" -Verb RunAs
        exit
    }
    catch {
        Write-Host "无法获取管理员权限: $_" -ForegroundColor Red
        Write-Host "将尝试继续执行，但可能会遇到权限问题" -ForegroundColor Yellow
    }
}

# 设置工作目录
Set-Location -Path "D:\Code\FileManager"

# 编译项目
Write-Host "正在编译项目..." -ForegroundColor Cyan
dotnet build -c Debug

# 检查编译是否成功
if ($LASTEXITCODE -eq 0) {
    Write-Host "编译成功!" -ForegroundColor Green
    Write-Host "正在以管理员权限启动应用程序..." -ForegroundColor Green
    
    # 启动应用程序（以管理员权限）
    $exePath = "D:\Code\FileManager\bin\Debug\net8.0-windows\FileManager.exe"
    if (Test-Path $exePath) {
        Write-Host "启动: $exePath" -ForegroundColor Cyan
        
        try {
            # 尝试以管理员权限启动应用程序
            Start-Process $exePath -Verb RunAs
            Write-Host "应用程序已成功启动!" -ForegroundColor Green
        }
        catch {
            Write-Host "启动应用程序时出错: $_" -ForegroundColor Red
            Write-Host "尝试直接启动应用程序（可能没有管理员权限）..." -ForegroundColor Yellow
            Start-Process $exePath
        }
    } 
    else {
        Write-Host "错误: 找不到可执行文件 $exePath" -ForegroundColor Red
        Write-Host "请检查编译输出目录是否正确。" -ForegroundColor Yellow
    }
} 
else {
    Write-Host "编译失败!" -ForegroundColor Red
    Write-Host "请修复上述错误后重试。" -ForegroundColor Yellow
} 