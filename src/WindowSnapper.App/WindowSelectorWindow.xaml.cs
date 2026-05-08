using System.Windows;
using System.Windows.Controls;
using WindowSnapper.App.ViewModels;

namespace WindowSnapper.App;

public partial class WindowSelectorWindow : Window
{
    internal WindowSelectorWindow(WindowSelectorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        viewModel.CloseRequested += OnCloseRequested;
        Closed += (_, _) => viewModel.CloseRequested -= OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        Close();
    }

    private void OnWindowSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not WindowSelectorViewModel viewModel)
        {
            return;
        }

        viewModel.UpdateSelectedWindows(WindowList.SelectedItems.OfType<WindowListItemViewModel>());
    }
}
