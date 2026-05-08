using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using WindowSnapper.App.ViewModels;

namespace WindowSnapper.App;

public partial class LayoutEditorWindow : Window
{
    private readonly LayoutEditorViewModel viewModel;

    internal LayoutEditorWindow(LayoutEditorViewModel viewModel)
    {
        this.viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += OnCloseRequested;
    }

    protected override void OnClosed(EventArgs e)
    {
        viewModel.CloseRequested -= OnCloseRequested;
        base.OnClosed(e);
    }

    private void OnCloseRequested(object? sender, EventArgs e)
    {
        Close();
    }

    private void OnZoneCanvasSizeChanged(object sender, SizeChangedEventArgs e)
    {
        viewModel.SetCanvasSize(e.NewSize.Width, e.NewSize.Height);
    }

    private void OnZoneMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: EditableZoneViewModel zone })
        {
            viewModel.SelectZone(zone);
        }
    }

    private void OnMoveThumbDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: EditableZoneViewModel zone })
        {
            viewModel.MoveZoneByPixels(zone, e.HorizontalChange, e.VerticalChange);
        }
    }

    private void OnResizeThumbDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: EditableZoneViewModel zone })
        {
            viewModel.ResizeZoneByPixels(zone, e.HorizontalChange, e.VerticalChange);
        }
    }

    private void OnZoneListSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox { SelectedItem: EditableZoneViewModel zone })
        {
            viewModel.SelectZone(zone);
        }
    }
}
