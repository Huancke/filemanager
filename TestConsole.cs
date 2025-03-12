using System;
using System.Diagnostics;
using System.IO;

namespace TestConsoleApp
{
    public class TestProgram
    {
        static void Main()
        {
            try
            {
                Console.WriteLine("测试控制台程序启动");
                
                // 尝试启动文件管理器
                string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FileManager.exe");
                Console.WriteLine($"尝试启动: {exePath}");
                
                if (File.Exists(exePath))
                {
                    Console.WriteLine("文件存在，正在启动...");
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = true
                    });
                    Console.WriteLine("启动命令已执行");
                }
                else
                {
                    Console.WriteLine($"文件不存在: {exePath}");
                }
                
                Console.WriteLine("按任意键退出...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.ReadKey();
            }
        }
    }
} 