using System;
using System.Windows;
using System.Windows.Controls;

namespace TestWpfApp
{
    public class Program
    {
        [STAThread]
        public static void Main()
        {
            var app = new Application();
            var window = new Window
            {
                Title = "测试WPF窗口",
                Width = 400,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
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

            app.Run(window);
        }
    }
} 