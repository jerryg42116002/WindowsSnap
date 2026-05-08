using System.Windows;
using WindowSnapper.App.ViewModels;

namespace WindowSnapper.App;

public partial class MainWindow : Window
{
    internal MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }
}
