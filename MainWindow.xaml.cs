using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SimpleFileManager2.ViewModel;

namespace SimpleFileManager2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FileManagerViewModel _viewModel;

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
                if (FileListView.SelectedItem is FileItem selectedItem)
                {
                    if (selectedItem.Type == "文件夹")
                    {
                        string newPath = System.IO.Path.Combine(_viewModel.CurrentPath, selectedItem.Name);
                        _viewModel.NavigateTo(newPath);
                    }
                    else
                    {
                        try
                        {
                            string filePath = System.IO.Path.Combine(_viewModel.CurrentPath, selectedItem.Name);
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = filePath,
                                UseShellExecute = true
                            });
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"无法打开文件: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
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
            _viewModel.IsStatusVisible = false;
        }

        private void PathTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _viewModel.NavigateTo(PathTextBox.Text);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.NavigateBack();
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.NavigateForward();
        }

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.NavigateUp();
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.NavigateTo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.RefreshCurrentDirectory();
        }

        private void NewFolderButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.CreateNewFolder();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("搜索功能尚未实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SortButton_Click(object sender, RoutedEventArgs e)
        {
            var contextMenu = new ContextMenu();
            
            var nameMenuItem = new MenuItem { Header = "按名称排序" };
            nameMenuItem.Click += (s, args) => _viewModel.SortCommand.Execute("Name");
            contextMenu.Items.Add(nameMenuItem);
            
            var typeMenuItem = new MenuItem { Header = "按类型排序" };
            typeMenuItem.Click += (s, args) => _viewModel.SortCommand.Execute("Type");
            contextMenu.Items.Add(typeMenuItem);
            
            var sizeMenuItem = new MenuItem { Header = "按大小排序" };
            sizeMenuItem.Click += (s, args) => _viewModel.SortCommand.Execute("Size");
            contextMenu.Items.Add(sizeMenuItem);
            
            var dateMenuItem = new MenuItem { Header = "按修改日期排序" };
            dateMenuItem.Click += (s, args) => _viewModel.SortCommand.Execute("ModifiedDate");
            contextMenu.Items.Add(dateMenuItem);
            
            contextMenu.IsOpen = true;
        }

        private void ViewButton_Click(object sender, RoutedEventArgs e)
        {
            var contextMenu = new ContextMenu();
            
            var listMenuItem = new MenuItem { Header = "列表" };
            listMenuItem.Click += (s, args) => _viewModel.CurrentViewMode = FileManagerViewModel.ViewMode.List;
            contextMenu.Items.Add(listMenuItem);
            
            var detailsMenuItem = new MenuItem { Header = "详细信息" };
            detailsMenuItem.Click += (s, args) => _viewModel.CurrentViewMode = FileManagerViewModel.ViewMode.Details;
            contextMenu.Items.Add(detailsMenuItem);
            
            var tilesMenuItem = new MenuItem { Header = "平铺" };
            tilesMenuItem.Click += (s, args) => _viewModel.CurrentViewMode = FileManagerViewModel.ViewMode.Tiles;
            contextMenu.Items.Add(tilesMenuItem);
            
            contextMenu.IsOpen = true;
        }

        private void DesktopButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.NavigateToSpecialFolder(Environment.SpecialFolder.Desktop);
        }

        private void DocumentsButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.NavigateToSpecialFolder(Environment.SpecialFolder.MyDocuments);
        }

        private void DownloadsButton_Click(object sender, RoutedEventArgs e)
        {
            string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            if (Directory.Exists(downloadsPath))
            {
                _viewModel.NavigateTo(downloadsPath);
            }
            else
            {
                MessageBox.Show("下载文件夹不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PicturesButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.NavigateToSpecialFolder(Environment.SpecialFolder.MyPictures);
        }

        private void MusicButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.NavigateToSpecialFolder(Environment.SpecialFolder.MyMusic);
        }

        private void VideosButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.NavigateToSpecialFolder(Environment.SpecialFolder.MyVideos);
        }

        private void ThisPCButton_Click(object sender, RoutedEventArgs e)
        {
            // 显示所有驱动器
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady).ToList();
            if (drives.Count > 0)
            {
                _viewModel.NavigateTo(drives[0].RootDirectory.FullName);
            }
        }

        private void CDriveButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.NavigateToDrive("C");
        }

        private void DDriveButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.NavigateToDrive("D");
        }

        // 右键菜单事件处理
        private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (FileListView.SelectedItem is FileItem selectedItem)
            {
                if (selectedItem.Type == "文件夹")
                {
                    string newPath = System.IO.Path.Combine(_viewModel.CurrentPath, selectedItem.Name);
                    _viewModel.NavigateTo(newPath);
                }
                else
                {
                    try
                    {
                        string filePath = System.IO.Path.Combine(_viewModel.CurrentPath, selectedItem.Name);
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = filePath,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"无法打开文件: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void CopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("复制功能尚未实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("剪切功能尚未实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("粘贴功能尚未实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.DeleteCommand.CanExecute(FileListView.SelectedItems))
            {
                _viewModel.DeleteCommand.Execute(FileListView.SelectedItems);
            }
        }

        private void RenameMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("重命名功能尚未实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PropertiesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("属性功能尚未实现", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}