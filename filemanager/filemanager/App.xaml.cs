using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using System.IO;

namespace FileManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [STAThread]
        public static void Main()
        {
            App app = new App();
            app.InitializeComponent();
            app.Run();
        }
        
        public App()
        {
            Debug.WriteLine("App构造函数开始");
            
            // 设置全局异常处理
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                LogError($"未处理的异常: {ex?.Message}", ex);
                MessageBox.Show($"发生严重错误: {ex?.Message}\n\n{ex?.StackTrace}", 
                    "应用程序错误", MessageBoxButton.OK, MessageBoxImage.Error);
            };
            
            // 处理UI线程未捕获的异常
            DispatcherUnhandledException += (s, e) =>
            {
                LogError($"UI线程异常: {e.Exception.Message}", e.Exception);
                MessageBox.Show($"UI错误: {e.Exception.Message}\n\n{e.Exception.StackTrace}", 
                    "UI错误", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
            };
            
            // 处理任务异常
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                LogError($"任务异常: {e.Exception.Message}", e.Exception);
                e.SetObserved();
            };
            
            LogInfo("应用程序启动");
            Debug.WriteLine("App构造函数完成");
        }
        
        private void LogInfo(string message)
        {
            try
            {
                Debug.WriteLine($"[INFO] {DateTime.Now}: {message}");
                File.AppendAllText("FileManager.log", $"[INFO] {DateTime.Now}: {message}\n");
            }
            catch
            {
                // 忽略日志错误
            }
        }
        
        private void LogError(string message, Exception? ex = null)
        {
            try
            {
                Debug.WriteLine($"[ERROR] {DateTime.Now}: {message}");
                if (ex != null)
                {
                    Debug.WriteLine($"[ERROR] {ex.StackTrace}");
                    File.AppendAllText("FileManager.log", $"[ERROR] {DateTime.Now}: {message}\n{ex.StackTrace}\n");
                }
                else
                {
                    File.AppendAllText("FileManager.log", $"[ERROR] {DateTime.Now}: {message}\n");
                }
            }
            catch
            {
                // 忽略日志错误
            }
        }
    }
} 