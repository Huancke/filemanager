# 简化版启动脚本
# 编译并以管理员权限启动FileManager应用程序

# 设置工作目录
Set-Location -Path "D:\Code\FileManager"

# 编译项目
Write-Host "正在编译项目..." -ForegroundColor Cyan
dotnet build -c Debug

# 检查编译是否成功
if ($LASTEXITCODE -eq 0) {
    Write-Host "编译成功!" -ForegroundColor Green
    
    # 启动应用程序（以管理员权限）
    $exePath = "D:\Code\FileManager\bin\Debug\net8.0-windows\FileManager.exe"
    if (Test-Path $exePath) {
        Write-Host "正在以管理员权限启动应用程序..." -ForegroundColor Green
        Start-Process $exePath -Verb RunAs
        Write-Host "应用程序已启动!" -ForegroundColor Green
    }
    else {
        Write-Host "错误: 找不到可执行文件 $exePath" -ForegroundColor Red
    }
}
else {
    Write-Host "编译失败!" -ForegroundColor Red
} 