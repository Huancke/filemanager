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

namespace SimpleFileManager2.ViewModel
{
    public class FileItem : INotifyPropertyChanged
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string ModifiedDate { get; set; } = string.Empty;
        public bool IsSystemFile { get; set; }
        public string FullPath { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public FileAttributes Attributes { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
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
                }
            }
        }

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
                    _forwardHistory.Clear();
                    _currentPath = value;
                    OnPropertyChanged(nameof(CurrentPath));
                    LoadCurrentDirectory();
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

        private ObservableCollection<FileItem> _fileItems = new ObservableCollection<FileItem>();
        public ObservableCollection<FileItem> FileItems
        {
            get => _fileItems;
            set
            {
                _fileItems = value;
                OnPropertyChanged(nameof(FileItems));
            }
        }

        public ICommand DeleteCommand { get; }
        public ICommand SortCommand { get; }

        public FileManagerViewModel()
        {
            DeleteCommand = new RelayCommand<object>(DeleteSelectedItems, CanDelete);
            SortCommand = new RelayCommand<string>(SortItems);
            
            _statusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _statusTimer.Tick += (s, e) =>
            {
                IsStatusVisible = false;
                _statusTimer.Stop();
            };
            
            // 默认加载用户目录
            CurrentPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        private bool CanDelete(object obj)
        {
            return obj != null;
        }

        public void LoadCurrentDirectory()
        {
            try
            {
                ShowStatus($"正在加载目录: {CurrentPath}");
                FileItems.Clear();

                if (!Directory.Exists(CurrentPath))
                {
                    ShowError($"目录不存在或无法访问: {CurrentPath}");
                    if (_pathHistory.Count > 0)
                    {
                        CurrentPath = _pathHistory.Pop();
                    }
                    else
                    {
                        CurrentPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    }
                    return;
                }

                ShowStatus($"正在枚举目录内容: {CurrentPath}");
                var entries = Directory.EnumerateFileSystemEntries(CurrentPath, "*", FileEnumOptions).ToList();

                if (entries.Count == 0)
                {
                    ShowStatus($"目录为空: {CurrentPath}");
                    return;
                }

                var directories = new List<FileItem>();
                var files = new List<FileItem>();

                foreach (var entry in entries)
                {
                    try
                    {
                        var info = new FileInfo(entry);
                        var isDirectory = (info.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
                        var isSystem = (info.Attributes & FileAttributes.System) == FileAttributes.System;
                        var isHidden = (info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;

                        var item = new FileItem
                        {
                            Name = Path.GetFileName(entry),
                            FullPath = entry,
                            IsSystemFile = isSystem || isHidden,
                            Attributes = info.Attributes,
                            ModifiedDate = File.GetLastWriteTime(entry).ToString("yyyy-MM-dd HH:mm:ss")
                        };

                        if (isDirectory)
                        {
                            item.Type = "文件夹";
                            item.Size = "";
                            item.Icon = "📁";
                            directories.Add(item);
                        }
                        else
                        {
                            var extension = Path.GetExtension(entry).ToLower();
                            item.Type = GetFileType(extension);
                            item.Size = GetFileSize(info.Length);
                            item.Icon = GetFileIcon(extension);
                            files.Add(item);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error processing entry {entry}: {ex.Message}");
                    }
                }

                // 先添加目录，再添加文件
                foreach (var dir in directories.OrderBy(d => d.Name))
                {
                    FileItems.Add(dir);
                }

                foreach (var file in files.OrderBy(f => f.Name))
                {
                    FileItems.Add(file);
                }

                ShowStatus($"已加载 {FileItems.Count} 个项目");
            }
            catch (Exception ex)
            {
                ShowError($"加载目录失败: {ex.Message}");
            }
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
                ShowError($"导航失败: {ex.Message}");
            }
        }

        public void NavigateTo(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    CurrentPath = path;
                }
                else
                {
                    ShowError($"目录不存在: {path}");
                }
            }
            catch (Exception ex)
            {
                ShowError($"导航失败: {ex.Message}");
            }
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
                    ShowError($"特殊文件夹不存在: {folder}");
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
                    ShowError($"驱动器不存在: {path}");
                }
            }
            catch (Exception ex)
            {
                ShowError($"导航到驱动器失败: {ex.Message}");
            }
        }

        public void RefreshCurrentDirectory()
        {
            LoadCurrentDirectory();
        }

        public void CreateNewFolder()
        {
            try
            {
                string baseFolderName = "新建文件夹";
                string newFolderName = baseFolderName;
                int counter = 1;

                string newFolderPath = Path.Combine(CurrentPath, newFolderName);
                while (Directory.Exists(newFolderPath))
                {
                    newFolderName = $"{baseFolderName} ({counter})";
                    newFolderPath = Path.Combine(CurrentPath, newFolderName);
                    counter++;
                }

                Directory.CreateDirectory(newFolderPath);
                ShowStatus($"已创建文件夹: {newFolderName}");
                RefreshCurrentDirectory();
            }
            catch (Exception ex)
            {
                ShowError($"创建文件夹失败: {ex.Message}");
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
            StatusMessage = $"错误: {message}";
            IsStatusVisible = true;
            _statusTimer.Stop();
            _statusTimer.Start();
        }

        private bool IsRunAsAdmin()
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void RequestAdminPrivilege()
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    WorkingDirectory = Environment.CurrentDirectory,
                    FileName = Process.GetCurrentProcess().MainModule.FileName,
                    Verb = "runas"
                };
                Process.Start(startInfo);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                ShowError($"请求管理员权限失败: {ex.Message}");
            }
        }

        private void DeleteSelectedItems(object parameter)
        {
            try
            {
                if (parameter is System.Collections.IList selectedItems && selectedItems.Count > 0)
                {
                    var itemsToDelete = selectedItems.Cast<FileItem>().ToList();
                    string message = itemsToDelete.Count == 1
                        ? $"确定要删除 {itemsToDelete[0].Name} 吗?"
                        : $"确定要删除这 {itemsToDelete.Count} 个项目吗?";

                    var result = MessageBox.Show(message, "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        int successCount = 0;
                        foreach (var item in itemsToDelete)
                        {
                            try
                            {
                                var fullPath = item.FullPath;
                                if ((item.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                                {
                                    Directory.Delete(fullPath, true);
                                }
                                else
                                {
                                    File.Delete(fullPath);
                                }
                                successCount++;
                            }
                            catch (Exception ex)
                            {
                                ShowError($"删除 {item.Name} 失败: {ex.Message}");
                            }
                        }

                        if (successCount > 0)
                        {
                            ShowStatus($"已删除 {successCount} 个项目");
                            RefreshCurrentDirectory();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError($"删除操作失败: {ex.Message}");
            }
        }

        private async Task UpdateFileSizeAsync(FileItem item)
        {
            try
            {
                if ((item.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    long size = await Task.Run(() => GetDirectorySize(item.FullPath));
                    item.Size = GetFileSize(size);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"计算文件夹大小失败: {ex.Message}");
            }
        }

        private long GetDirectorySize(string path)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(path);
                return dir.GetFiles("*", SearchOption.AllDirectories).Sum(fi => fi.Length);
            }
            catch
            {
                return 0;
            }
        }

        private string GetFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private string GetFileType(string extension)
        {
            return extension switch
            {
                ".txt" => "文本文件",
                ".doc" or ".docx" => "Word文档",
                ".xls" or ".xlsx" => "Excel表格",
                ".ppt" or ".pptx" => "PowerPoint演示文稿",
                ".pdf" => "PDF文档",
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "图片",
                ".mp3" or ".wav" or ".flac" or ".aac" => "音频",
                ".mp4" or ".avi" or ".mkv" or ".mov" => "视频",
                ".zip" or ".rar" or ".7z" => "压缩文件",
                ".exe" => "可执行文件",
                ".dll" => "动态链接库",
                _ => extension.Length > 0 ? extension.Substring(1).ToUpper() + "文件" : "未知文件类型"
            };
        }

        private string GetFileIcon(string extension)
        {
            return extension switch
            {
                ".txt" => "📄",
                ".doc" or ".docx" => "📝",
                ".xls" or ".xlsx" => "📊",
                ".ppt" or ".pptx" => "📑",
                ".pdf" => "📰",
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "🖼️",
                ".mp3" or ".wav" or ".flac" or ".aac" => "🎵",
                ".mp4" or ".avi" or ".mkv" or ".mov" => "🎬",
                ".zip" or ".rar" or ".7z" => "📦",
                ".exe" => "⚙️",
                ".dll" => "🔧",
                _ => "📄"
            };
        }

        private void SortItems(string property)
        {
            if (string.IsNullOrEmpty(property))
                return;

            if (property == _sortProperty)
            {
                _sortAscending = !_sortAscending;
            }
            else
            {
                _sortProperty = property;
                _sortAscending = true;
            }

            var view = CollectionViewSource.GetDefaultView(FileItems);
            view.SortDescriptions.Clear();

            // 始终保持文件夹在前面
            view.SortDescriptions.Add(new SortDescription("Type", _sortProperty == "Type" && !_sortAscending ? ListSortDirection.Ascending : ListSortDirection.Descending));

            // 然后按指定属性排序
            view.SortDescriptions.Add(new SortDescription(_sortProperty, _sortAscending ? ListSortDirection.Ascending : ListSortDirection.Descending));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute((T)parameter);
        }

        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}