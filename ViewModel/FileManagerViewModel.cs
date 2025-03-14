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
using System.Windows.Controls;
using System.Windows.Data;

namespace FileManager.ViewModel
{
    public class FileItem
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string ModifiedDate { get; set; } = string.Empty;
        public bool IsSystemFile { get; set; }
        public string FullPath { get; set; } = string.Empty;
        public FileAttributes Attributes { get; set; }
    }

    public class FileManagerViewModel : INotifyPropertyChanged
    {
        private readonly Stack<string> _pathHistory = new Stack<string>();
        private readonly Stack<string> _forwardHistory = new Stack<string>();
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
        private string _sortProperty = "Name";
        private bool _sortAscending = true;
        private ViewMode _currentViewMode = ViewMode.Details;
        
        public enum ViewMode
        {
            List,
            Details,
            Tiles
        }
        
        public ViewMode CurrentViewMode
        {
            get => _currentViewMode;
            set
            {
                if (_currentViewMode != value)
                {
                    _currentViewMode = value;
                    OnPropertyChanged(nameof(CurrentViewMode));
                    OnPropertyChanged(nameof(CurrentView));
                }
            }
        }
        
        public GridView CurrentView
        {
            get
            {
                var view = new GridView();
                switch (CurrentViewMode)
                {
                    case ViewMode.List:
                        view.Columns.Add(new GridViewColumn
                        {
                            Header = "名称",
                            Width = 400,
                            CellTemplate = Application.Current.FindResource("NameColumnTemplate") as DataTemplate
                        });
                        break;
                    case ViewMode.Tiles:
                        view.Columns.Add(new GridViewColumn
                        {
                            Width = 120,
                            CellTemplate = Application.Current.FindResource("TileColumnTemplate") as DataTemplate
                        });
                        break;
                    case ViewMode.Details:
                    default:
                        view.Columns.Add(new GridViewColumn
                        {
                            Header = "名称",
                            Width = 250,
                            CellTemplate = Application.Current.FindResource("NameColumnTemplate") as DataTemplate
                        });
                        view.Columns.Add(new GridViewColumn
                        {
                            Header = "修改日期",
                            Width = 150,
                            DisplayMemberBinding = new Binding("ModifiedDate")
                        });
                        view.Columns.Add(new GridViewColumn
                        {
                            Header = "类型",
                            Width = 100,
                            DisplayMemberBinding = new Binding("Type")
                        });
                        view.Columns.Add(new GridViewColumn
                        {
                            Header = "大小",
                            Width = 100,
                            DisplayMemberBinding = new Binding("Size")
                        });
                        view.Columns.Add(new GridViewColumn
                        {
                            Header = "系统文件",
                            Width = 80,
                            CellTemplate = Application.Current.FindResource("SystemFileColumnTemplate") as DataTemplate
                        });
                        break;
                }
                return view;
            }
        }

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
                        _forwardHistory.Clear(); // 清除前进历史
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

        public ICommand DeleteCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand NavigateUpCommand { get; }
        public ICommand NavigateForwardCommand { get; }
        public ICommand CreateFolderCommand { get; }
        public ICommand SortCommand { get; }

        public FileManagerViewModel()
        {
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
                RefreshCommand = new RelayCommand(_ => RefreshCurrentDirectory());
                NavigateUpCommand = new RelayCommand(_ => NavigateUp());
                NavigateForwardCommand = new RelayCommand(_ => NavigateForward(), _ => _forwardHistory.Count > 0);
                CreateFolderCommand = new RelayCommand(_ => CreateNewFolder());
                SortCommand = new RelayCommand(SortItems);
                
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
                            
                            // 获取文件或目录的修改日期
                            DateTime modifiedTime = File.GetLastWriteTime(entry);
                            string modifiedDateStr = modifiedTime.ToString("yyyy/MM/dd HH:mm");

                            var fileItem = new FileItem
                            {
                                Name = Path.GetFileName(entry),
                                Type = isDirectory ? "文件夹" : "文件",
                                Size = "计算中...",
                                ModifiedDate = modifiedDateStr,
                                IsSystemFile = isSystem,
                                FullPath = entry,
                                Attributes = attr
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
                                ModifiedDate = "N/A",
                                IsSystemFile = true,
                                FullPath = entry
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

        private bool IsAdminRequired(string path)
        {
            try 
            {
                // 检查是否是系统目录或C盘
                string systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                
                return path.StartsWith(systemRoot, StringComparison.OrdinalIgnoreCase) ||
                       path.StartsWith(programFiles, StringComparison.OrdinalIgnoreCase) ||
                       Path.GetPathRoot(path)?.StartsWith("C:\\", StringComparison.OrdinalIgnoreCase) == true;
            }
            catch 
            {
                return false;
            }
        }

        private bool IsRunAsAdmin()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void RequestAdminPrivilege()
        {
            var processInfo = new ProcessStartInfo
            {
                Verb = "runas",
                FileName = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty,
                UseShellExecute = true
            };

            try
            {
                Process.Start(processInfo);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                ShowError("需要管理员权限执行此操作: " + ex.Message);
            }
        }

        private async void DeleteSelectedItems(object? parameter)
        {
            if (parameter is not IList<object> selectedItems || selectedItems.Count == 0)
                return;

            var items = new List<FileItem>();
            foreach (var item in selectedItems)
            {
                if (item is FileItem fileItem)
                    items.Add(fileItem);
            }

            foreach (var item in items)
            {
                try
                {
                    var fullPath = Path.Combine(CurrentPath, item.Name);
                    if (IsAdminRequired(fullPath) && !IsRunAsAdmin())
                    {
                        RequestAdminPrivilege();
                        return;
                    }

                    await Task.Run(async () => 
                    {
                        try
                        {
                            if (item.Type == "文件夹")
                            {
                                Directory.Delete(fullPath, true);
                            }
                            else
                            {
                                File.Delete(fullPath);
                            }

                            await Application.Current.Dispatcher.InvokeAsync(() => 
                            {
                                FileItems.Remove(item);
                            });
                        }
                        catch (Exception ex)
                        {
                            await Application.Current.Dispatcher.InvokeAsync(() => 
                            {
                                ShowError($"删除失败: {ex.Message}");
                            });
                        }
                    });
                }
                catch (Exception ex)
                {
                    ShowError($"删除操作失败: {ex.Message}");
                }
            }
        }

        private async Task UpdateFileSizeAsync(string path, FileItem item, bool isDirectory)
        {
            try
            {
                if (isDirectory)
                {
                    // 计算文件夹大小（这是一个耗时操作）
                    await Task.Run(async () => 
                    {
                        try
                        {
                            long size = CalculateFolderSize(path);
                            await Application.Current.Dispatcher.InvokeAsync(() => 
                            {
                                item.Size = FormatSize(size);
                            });
                        }
                        catch
                        {
                            await Application.Current.Dispatcher.InvokeAsync(() => 
                            {
                                item.Size = "无法计算";
                            });
                        }
                    });
                }
                else
                {
                    // 计算文件大小
                    var info = new FileInfo(path);
                    long size = info.Length;
                    await Application.Current.Dispatcher.InvokeAsync(() => 
                    {
                        item.Size = FormatSize(size);
                    });
                }
            }
            catch (Exception)
            {
                await Application.Current.Dispatcher.InvokeAsync(() => 
                {
                    item.Size = "无法读取";
                });
            }
        }

        private long CalculateFolderSize(string folder)
        {
            long size = 0;
            try
            {
                // 计算所有文件的大小
                var files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var info = new FileInfo(file);
                    size += info.Length;
                }
            }
            catch
            {
                // 忽略无法访问的文件
            }
            return size;
        }

        private string FormatSize(long bytes)
        {
            return bytes switch
            {
                < 1024 => $"{bytes} B",
                < 1024 * 1024 => $"{bytes / 1024:N0} KB",
                < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024.0):N2} MB",
                _ => $"{bytes / (1024.0 * 1024.0 * 1024.0):N2} GB"
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void NavigateBack()
        {
            if (_pathHistory.Count > 0)
            {
                string previousPath = _pathHistory.Pop();
                _forwardHistory.Push(_currentPath); // 保存当前路径到前进历史
                _currentPath = previousPath;
                OnPropertyChanged(nameof(CurrentPath));
                LoadCurrentDirectory();
            }
        }

        public void NavigateForward()
        {
            if (_forwardHistory.Count > 0)
            {
                string nextPath = _forwardHistory.Pop();
                _pathHistory.Push(_currentPath); // 保存当前路径到后退历史
                _currentPath = nextPath;
                OnPropertyChanged(nameof(CurrentPath));
                LoadCurrentDirectory();
            }
        }

        public void NavigateUp()
        {
            try
            {
                DirectoryInfo parent = Directory.GetParent(CurrentPath);
                if (parent != null)
                {
                    NavigateTo(parent.FullName);
                }
            }
            catch (Exception ex)
            {
                ShowError($"无法导航到上级目录: {ex.Message}");
            }
        }

        public void RefreshCurrentDirectory()
        {
            LoadCurrentDirectory();
        }

        public void NavigateToSpecialFolder(Environment.SpecialFolder folder)
        {
            try
            {
                string path = Environment.GetFolderPath(folder);
                if (Directory.Exists(path))
                {
                    NavigateTo(path);
                }
                else
                {
                    ShowError($"无法访问特殊文件夹: {folder}");
                }
            }
            catch (Exception ex)
            {
                ShowError($"导航到特殊文件夹失败: {ex.Message}");
            }
        }

        public void NavigateToDrive(string driveLetter)
        {
            try
            {
                string path = $"{driveLetter}:\\";
                if (Directory.Exists(path))
                {
                    NavigateTo(path);
                }
                else
                {
                    ShowError($"无法访问驱动器: {driveLetter}");
                }
            }
            catch (Exception ex)
            {
                ShowError($"导航到驱动器失败: {ex.Message}");
            }
        }

        public void CreateNewFolder()
        {
            try
            {
                int counter = 1;
                string baseName = "新建文件夹";
                string newFolderName = baseName;
                string newFolderPath = Path.Combine(CurrentPath, newFolderName);

                // 检查是否已存在同名文件夹，如果存在则添加数字后缀
                while (Directory.Exists(newFolderPath))
                {
                    counter++;
                    newFolderName = $"{baseName} ({counter})";
                    newFolderPath = Path.Combine(CurrentPath, newFolderName);
                }

                // 创建新文件夹
                Directory.CreateDirectory(newFolderPath);
                ShowStatus($"已创建文件夹: {newFolderName}");

                // 刷新当前目录
                RefreshCurrentDirectory();
            }
            catch (Exception ex)
            {
                ShowError($"创建文件夹失败: {ex.Message}");
            }
        }

        private void SortItems(object parameter)
        {
            string property = parameter as string ?? "Name";
            
            // 如果点击相同的属性，则切换排序方向
            if (property == _sortProperty)
            {
                _sortAscending = !_sortAscending;
            }
            else
            {
                _sortProperty = property;
                _sortAscending = true;
            }
            
            // 创建一个临时列表进行排序
            List<FileItem> sortedItems = new List<FileItem>(FileItems);
            
            // 根据属性和排序方向进行排序
            switch (_sortProperty)
            {
                case "Name":
                    sortedItems = _sortAscending 
                        ? sortedItems.OrderBy(f => f.Type).ThenBy(f => f.Name).ToList()
                        : sortedItems.OrderBy(f => f.Type).ThenByDescending(f => f.Name).ToList();
                    break;
                case "Size":
                    sortedItems = _sortAscending 
                        ? sortedItems.OrderBy(f => f.Type).ThenBy(f => GetSizeValue(f.Size)).ToList()
                        : sortedItems.OrderBy(f => f.Type).ThenByDescending(f => GetSizeValue(f.Size)).ToList();
                    break;
                case "Type":
                    sortedItems = _sortAscending 
                        ? sortedItems.OrderBy(f => f.Type).ToList()
                        : sortedItems.OrderByDescending(f => f.Type).ToList();
                    break;
                case "ModifiedDate":
                    sortedItems = _sortAscending 
                        ? sortedItems.OrderBy(f => f.Type).ThenBy(f => GetDateValue(f.ModifiedDate)).ToList()
                        : sortedItems.OrderBy(f => f.Type).ThenByDescending(f => GetDateValue(f.ModifiedDate)).ToList();
                    break;
            }
            
            // 清除并重新添加排序后的项目
            FileItems.Clear();
            foreach (var item in sortedItems)
            {
                FileItems.Add(item);
            }
            
            ShowStatus($"已按{GetSortPropertyDisplayName(_sortProperty)}{(_sortAscending ? "升序" : "降序")}排序");
        }
        
        private string GetSortPropertyDisplayName(string property)
        {
            return property switch
            {
                "Name" => "名称",
                "Size" => "大小",
                "Type" => "类型",
                "ModifiedDate" => "修改日期",
                _ => property
            };
        }
        
        private long GetSizeValue(string sizeStr)
        {
            if (sizeStr == "计算中..." || sizeStr == "N/A")
                return 0;
            
            // 尝试解析大小字符串
            try
            {
                if (sizeStr.Contains("KB"))
                {
                    double kb = double.Parse(sizeStr.Replace("KB", "").Trim());
                    return (long)(kb * 1024);
                }
                else if (sizeStr.Contains("MB"))
                {
                    double mb = double.Parse(sizeStr.Replace("MB", "").Trim());
                    return (long)(mb * 1024 * 1024);
                }
                else if (sizeStr.Contains("GB"))
                {
                    double gb = double.Parse(sizeStr.Replace("GB", "").Trim());
                    return (long)(gb * 1024 * 1024 * 1024);
                }
                else if (sizeStr.Contains("B"))
                {
                    return long.Parse(sizeStr.Replace("B", "").Trim());
                }
            }
            catch
            {
                // 解析失败，返回0
                return 0;
            }
            
            return 0;
        }
        
        private DateTime GetDateValue(string dateStr)
        {
            if (dateStr == "N/A")
                return DateTime.MinValue;
            
            try
            {
                return DateTime.Parse(dateStr);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        public void NavigateTo(string newPath)
        {
            if (Directory.Exists(newPath))
            {
                _pathHistory.Push(CurrentPath);
                CurrentPath = newPath;
                LoadCurrentDirectory();
            }
            else
            {
                ShowError($"目录不存在: {newPath}");
            }
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);
        
        public void Execute(object? parameter) => _execute(parameter ?? new object());
        
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}