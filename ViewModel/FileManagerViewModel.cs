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

namespace FileManager.ViewModel
{
    public class FileItem
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Size { get; set; }
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

        public FileManagerViewModel()
        {
            DeleteCommand = new RelayCommand(DeleteSelectedItems, CanDelete);
            CurrentPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            LoadCurrentDirectory();
        }

        private bool CanDelete(object parameter)
        {
            return parameter is IList<object> items && items.Count > 0;
        }

        private void LoadCurrentDirectory()
        {
            FileItems.Clear();

            try
            {
                // 检查目录是否存在
                if (!Directory.Exists(CurrentPath))
                {
                    MessageBox.Show($"目录不存在或无法访问: {CurrentPath}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    var entries = Directory.EnumerateFileSystemEntries(CurrentPath, "*", FileEnumOptions).ToList();
                    
                    if (entries.Count == 0)
                    {
                        // 目录为空或无法访问内容
                        MessageBox.Show($"目录为空或无法访问内容: {CurrentPath}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
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
                }
                catch (UnauthorizedAccessException uaEx)
                {
                    MessageBox.Show($"访问被拒绝: {uaEx.Message}\n\n请确保应用程序以管理员权限运行。", 
                        "权限错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    
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
                MessageBox.Show($"加载目录失败: {ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}", 
                    "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                FileName = Process.GetCurrentProcess().MainModule.FileName,
                UseShellExecute = true
            };

            try
            {
                Process.Start(processInfo);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show("需要管理员权限执行此操作: " + ex.Message, "权限错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void DeleteSelectedItems(object parameter)
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
                                MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            });
                        }
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"删除操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private string _currentPath;
        public string CurrentPath
        {
            get => _currentPath;
            set
            {
                _currentPath = value;
                OnPropertyChanged(nameof(CurrentPath));
            }
        }

        public ObservableCollection<FileItem> FileItems { get; } = new ObservableCollection<FileItem>();

        public ICommand DeleteCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void NavigateBack()
        {
            if (_pathHistory.TryPop(out var previousPath))
            {
                CurrentPath = previousPath;
                LoadCurrentDirectory();
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
                MessageBox.Show($"目录不存在: {newPath}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);
        
        public void Execute(object parameter) => _execute(parameter);
        
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}