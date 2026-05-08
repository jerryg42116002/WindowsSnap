using WindowSnapper.Layouts;

namespace WindowSnapper.App.ViewModels;

internal sealed class LayoutChoiceViewModel
{
    public LayoutChoiceViewModel(LayoutDefinition layout)
    {
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
    }

    public LayoutDefinition Layout { get; }

    public string Id => Layout.Id;

    public string Name => Layout.Name;
}
