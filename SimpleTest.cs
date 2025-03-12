using System;
using System.Windows;
using System.Windows.Controls;

namespace SimpleTest
{
    public class Program
    {
        [STAThread]
        public static void Main()
        {
            try
            {
                var app = new Application();
                var window = new Window
                {
                    Title = "简单测试窗口",
                    Width = 400,
                    Height = 300,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Topmost = true
                };

                var grid = new Grid();
                var button = new Button
                {
                    Content = "点击我",
                    Width = 100,
                    Height = 30
                };

                button.Click += (s, e) => MessageBox.Show("WPF工作正常！");
                grid.Children.Add(button);
                window.Content = grid;

                window.Loaded += (s, e) => 
                {
                    window.Activate();
                    window.Focus();
                    MessageBox.Show("窗口已加载！", "测试", MessageBoxButton.OK, MessageBoxImage.Information);
                };

                app.Run(window);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"错误: {ex.Message}\n\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 