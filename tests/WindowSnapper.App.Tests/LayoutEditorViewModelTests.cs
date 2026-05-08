using WindowSnapper.App.ViewModels;
using WindowSnapper.Core.Results;
using WindowSnapper.Layouts;
using Xunit;

namespace WindowSnapper.App.Tests;

public sealed class LayoutEditorViewModelTests
{
    [Fact]
    public void AddZoneCommandCreatesUniqueZones()
    {
        var viewModel = CreateViewModel();

        for (var index = 0; index < 5; index++)
        {
            viewModel.AddZoneCommand.Execute(null);
        }

        Assert.Equal(6, viewModel.Zones.Count);
        Assert.Equal(6, viewModel.Zones.Select(zone => zone.Id).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void MoveZoneByPixelsClampsZoneInsideCanvas()
    {
        var viewModel = CreateViewModel();
        viewModel.SetCanvasSize(1000, 500);
        var zone = viewModel.SelectedZone!;

        viewModel.MoveZoneByPixels(zone, 10_000, 10_000);

        Assert.Equal(1 - zone.Width, zone.X, precision: 6);
        Assert.Equal(1 - zone.Height, zone.Y, precision: 6);
    }

    [Fact]
    public void ResizeZoneByPixelsKeepsMinimumSize()
    {
        var viewModel = CreateViewModel();
        viewModel.SetCanvasSize(1000, 500);
        var zone = viewModel.SelectedZone!;

        viewModel.ResizeZoneByPixels(zone, -10_000, -10_000);

        Assert.Equal(0.02, zone.Width, precision: 6);
        Assert.Equal(0.02, zone.Height, precision: 6);
    }

    [Fact]
    public void ToLayoutDefinitionCreatesValidLayoutForSixZones()
    {
        var viewModel = CreateViewModel();
        viewModel.LayoutId = "six-grid";
        viewModel.LayoutName = "Six Grid";
        for (var index = 0; index < 5; index++)
        {
            viewModel.AddZoneCommand.Execute(null);
        }

        var layout = viewModel.ToLayoutDefinition();
        var validation = new LayoutValidator().Validate(layout);

        Assert.True(validation.IsSuccess);
        Assert.Equal(6, layout.Zones.Count);
    }

    [Fact]
    public async Task SaveAsyncRejectsBuiltinLayoutId()
    {
        var saved = false;
        var viewModel = CreateViewModel(_ =>
        {
            saved = true;
            return Task.FromResult(Result.Success());
        });
        viewModel.LayoutId = BuiltinLayouts.LeftHalfId;

        var result = await viewModel.SaveAsync();

        Assert.False(result.IsSuccess);
        Assert.False(saved);
        Assert.Contains("内置布局", viewModel.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveAsyncCallsSaveDelegateForValidLayout()
    {
        LayoutDefinition? savedLayout = null;
        var viewModel = CreateViewModel(layout =>
        {
            savedLayout = layout;
            return Task.FromResult(Result.Success());
        });
        viewModel.LayoutId = "dev-grid";
        viewModel.LayoutName = "Dev Grid";

        var result = await viewModel.SaveAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(savedLayout);
        Assert.Equal("dev-grid", savedLayout.Id);
    }

    private static LayoutEditorViewModel CreateViewModel(Func<LayoutDefinition, Task<Result>>? saveLayoutAsync = null)
    {
        return new LayoutEditorViewModel(saveLayoutAsync ?? (_ => Task.FromResult(Result.Success())));
    }
}
