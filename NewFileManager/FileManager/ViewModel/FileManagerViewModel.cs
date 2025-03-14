using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace FileManager.ViewModel
{
    public class FileItem
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public bool IsSystemFile { get; set; }
    }

    public class FileManagerViewModel : INotifyPropertyChanged
    {
        private readonly Stack<string> _pathHistory = new Stack<string>();
        private static readonly EnumerationOptions FileEnumOptions = new EnumerationOptions
        {
            AttributesToSkip = FileAttributes.Normal,
            RecurseSubdirectories = false,
            IgnoreInaccessible = true
        };

        private string _currentPath = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _isStatusVisible = false;
        private readonly DispatcherTimer _statusTimer;

        public ObservableCollection<FileItem> FileItems { get; } = new ObservableCollection<FileItem>();

        public string CurrentPath
        {
            get => _currentPath;
            set
            {
                if (_currentPath != value)
                {
                    if (!string.IsNullOrEmpty(_currentPath))
                    {
                        _pathHistory.Push(_currentPath);
                    }
                    _currentPath = value;
                    OnPropertyChanged(nameof(CurrentPath));
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged(nameof(StatusMessage));
                }
            }
        }

        public bool IsStatusVisible
        {
            get => _isStatusVisible;
            set
            {
                if (_isStatusVisible != value)
                {
                    _isStatusVisible = value;
                    OnPropertyChanged(nameof(IsStatusVisible));
                }
            }
        }

        public ICommand DeleteCommand { get; private set; }

        public FileManagerViewModel()
        {
            DeleteCommand = new RelayCommand(DeleteSelectedFiles);
            _statusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _statusTimer.Tick += (s, e) =>
            {
                IsStatusVisible = false;
                _statusTimer.Stop();
            };

            try
            {
                // 显示状态信息
                StatusMessage = "正在初始化文件管理器...";
                IsStatusVisible = true;
                
                // 初始化命令
                DeleteCommand = new RelayCommand(DeleteSelectedItems, CanDelete);
                
                // 设置初始路径
                CurrentPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                
                // 延迟加载目录内容
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    LoadCurrentDirectory();
                }));
            }
            catch (Exception ex)
            {
                ShowError($"初始化失败: {ex.Message}");
            }
        }

        private bool CanDelete(object? parameter)
        {
            return parameter is IList<object> items && items.Count > 0;
        }

        private void LoadCurrentDirectory()
        {
            try
            {
                ShowStatus($"正在加载目录: {CurrentPath}");
                FileItems.Clear();

                // 检查目录是否存在
                if (!Directory.Exists(CurrentPath))
                {
                    ShowError($"目录不存在或无法访问: {CurrentPath}");
                    if (_pathHistory.Count > 0)
                    {
                        // 返回上一个目录
                        CurrentPath = _pathHistory.Pop();
                    }
                    else
                    {
                        // 如果没有历史记录，返回用户目录
                        CurrentPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    }
                    return;
                }

                // 尝试列出目录内容
                try
                {
                    ShowStatus($"正在枚举目录内容: {CurrentPath}");
                    var entries = Directory.EnumerateFileSystemEntries(CurrentPath, "*", FileEnumOptions).ToList();
                    
                    ShowStatus($"找到 {entries.Count} 个项目");
                    
                    if (entries.Count == 0)
                    {
                        // 目录为空或无法访问内容
                        ShowStatus($"目录为空: {CurrentPath}");
                    }
                    
                    foreach (var entry in entries)
                    {
                        try
                        {
                            var attr = File.GetAttributes(entry);
                            var isDirectory = (attr & FileAttributes.Directory) == FileAttributes.Directory;
                            var isSystem = (attr & FileAttributes.System) == FileAttributes.System;

                            var fileItem = new FileItem
                            {
                                Name = Path.GetFileName(entry),
                                Type = isDirectory ? "文件夹" : "文件",
                                Size = "计算中...",
                                IsSystemFile = isSystem
                            };

                            FileItems.Add(fileItem);
                            
                            // 异步计算文件或文件夹大小
                            Task.Run(() => UpdateFileSizeAsync(entry, fileItem, isDirectory));
                        }
                        catch (Exception itemEx)
                        {
                            // 单个项目处理失败，继续处理其他项目
                            Debug.WriteLine($"处理项目失败: {entry}, 错误: {itemEx.Message}");
                            
                            // 添加一个错误项目
                            FileItems.Add(new FileItem
                            {
                                Name = Path.GetFileName(entry) + " (访问受限)",
                                Type = "错误",
                                Size = "N/A",
                                IsSystemFile = true
                            });
                        }
                    }
                    
                    ShowStatus($"已加载 {FileItems.Count} 个项目");
                }
                catch (UnauthorizedAccessException uaEx)
                {
                    ShowError($"访问被拒绝: {uaEx.Message}\n\n请确保应用程序以管理员权限运行。");
                    
                    if (!IsRunAsAdmin())
                    {
                        var result = MessageBox.Show("是否以管理员权限重新启动应用程序？", 
                            "权限不足", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        
                        if (result == MessageBoxResult.Yes)
                        {
                            RequestAdminPrivilege();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError($"加载目录失败: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private void ShowStatus(string message)
        {
            StatusMessage = message;
            IsStatusVisible = true;
            _statusTimer.Stop();
            _statusTimer.Start();
        }

        private void ShowError(string message)
        {
            StatusMessage = "错误: " + message;
            IsStatusVisible = true;
            _statusTimer.Stop();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}