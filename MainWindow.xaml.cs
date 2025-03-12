using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using FileManager.ViewModel;

namespace FileManager;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private FileManagerViewModel? _viewModel;
    
    public MainWindow()
    {
        try
        {
            Debug.WriteLine("开始初始化MainWindow");
            InitializeComponent();
            
            Debug.WriteLine("InitializeComponent完成");
            
            // 确保UI线程异常被捕获
            Application.Current.DispatcherUnhandledException += (s, e) => 
            {
                MessageBox.Show($"发生错误: {e.Exception.Message}\n\n{e.Exception.StackTrace}", 
                    "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
            };
            
            Debug.WriteLine("创建ViewModel");
            _viewModel = new FileManagerViewModel();
            DataContext = _viewModel;
            Debug.WriteLine("设置DataContext完成");
            
            // 设置事件处理
            BackButton.Click += (s, e) => _viewModel.NavigateBack();
            PathTextBox.KeyDown += (s, e) => {
                if (e.Key == Key.Enter)
                    _viewModel.NavigateTo(PathTextBox.Text);
            };
            FileListView.MouseDoubleClick += FileListView_MouseDoubleClick;
            Debug.WriteLine("事件处理设置完成");
            
            // 确保窗口显示在前台
            Loaded += MainWindow_Loaded;
            
            Debug.WriteLine("MainWindow初始化完成");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"窗口初始化失败: {ex.Message}\n{ex.StackTrace}");
            MessageBox.Show($"窗口初始化失败: {ex.Message}\n\n{ex.StackTrace}", 
                "严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Debug.WriteLine("MainWindow_Loaded事件触发");
            
            // 确保窗口显示在前台
            Topmost = true;
            Activate();
            Focus();
            
            // 短暂延迟后取消Topmost，这样窗口不会一直保持在最前面
            Task.Delay(1000).ContinueWith(_ => 
            {
                Dispatcher.Invoke(() => 
                {
                    Topmost = false;
                    Debug.WriteLine("取消Topmost设置");
                });
            });
            
            Debug.WriteLine("MainWindow_Loaded事件处理完成");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Loaded事件处理失败: {ex.Message}");
        }
    }

    private void FileListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        try
        {
            if (_viewModel != null && FileListView.SelectedItem is FileItem selectedItem && selectedItem.Type == "文件夹")
            {
                string newPath = System.IO.Path.Combine(_viewModel.CurrentPath, selectedItem.Name);
                _viewModel.NavigateTo(newPath);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"处理双击事件失败: {ex.Message}");
            MessageBox.Show($"处理双击事件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private void CloseStatusButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.IsStatusVisible = false;
        }
    }
}