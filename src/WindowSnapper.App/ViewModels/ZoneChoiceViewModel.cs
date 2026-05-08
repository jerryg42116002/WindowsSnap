using WindowSnapper.Layouts;

namespace WindowSnapper.App.ViewModels;

internal sealed class ZoneChoiceViewModel
{
    public ZoneChoiceViewModel(ZoneDefinition zone, int sortedIndex)
    {
        Zone = zone ?? throw new ArgumentNullException(nameof(zone));
        SortedIndex = sortedIndex;
    }

    public ZoneDefinition Zone { get; }

    public int SortedIndex { get; }

    public string Id => Zone.Id;

    public string Name => Zone.Name;

    public string DisplayName => $"第 {SortedIndex} 区：{Name}";
}
