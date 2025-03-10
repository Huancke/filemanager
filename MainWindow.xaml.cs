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
using FileManager.ViewModel;

namespace FileManager;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly FileManagerViewModel _viewModel = new FileManagerViewModel();
    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        BackButton.Click += (s, e) => _viewModel.NavigateBack();
        PathTextBox.KeyDown += (s, e) => {
            if (e.Key == Key.Enter)
                _viewModel.NavigateTo(PathTextBox.Text);
        };
        FileListView.MouseDoubleClick += FileListView_MouseDoubleClick;
    }

    private void FileListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FileListView.SelectedItem is FileItem selectedItem && selectedItem.Type == "文件夹")
        {
            string newPath = System.IO.Path.Combine(_viewModel.CurrentPath, selectedItem.Name);
            _viewModel.NavigateTo(newPath);
        }
    }
}