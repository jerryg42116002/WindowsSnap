using WindowSnapper.Core.Windows;

namespace WindowSnapper.App.ViewModels;

internal sealed class WindowListItemViewModel : ViewModelBase
{
    private const int MaxTitleLength = 80;
    private string? assignedLayoutId;
    private string? assignedLayoutName;
    private string? assignedZoneId;
    private string? assignedZoneDisplayName;

    private WindowListItemViewModel(WindowInfo window)
    {
        Handle = window.Handle;
        Title = CreateTitlePreview(window.Title);
        ProcessName = string.IsNullOrWhiteSpace(window.ProcessName) ? "unknown" : window.ProcessName;
        ClassName = string.IsNullOrWhiteSpace(window.ClassName) ? "unknown" : window.ClassName;
        BoundsText = $"{window.Bounds.X},{window.Bounds.Y} {window.Bounds.Width}x{window.Bounds.Height}";
        IsVisible = window.IsVisible;
        IsMinimized = window.IsMinimized;
        StateText = !window.IsVisible ? "隐藏" : window.IsMinimized ? "最小化" : "正常";
    }

    public WindowHandle Handle { get; }

    public string Title { get; }

    public string ProcessName { get; }

    public string ClassName { get; }

    public string BoundsText { get; }

    public string StateText { get; }

    public bool IsVisible { get; }

    public bool IsMinimized { get; }

    public string Details => $"{ProcessName} · {ClassName} · {BoundsText} · {StateText}";

    public bool HasAssignment =>
        !string.IsNullOrWhiteSpace(assignedLayoutId) &&
        !string.IsNullOrWhiteSpace(assignedZoneId);

    public string AssignedLayoutId => assignedLayoutId ?? string.Empty;

    public string AssignedZoneId => assignedZoneId ?? string.Empty;

    public string AssignmentText => HasAssignment
        ? $"目标：{assignedLayoutName} / {assignedZoneDisplayName}"
        : "目标：未选定";

    public string DisplayName => string.IsNullOrWhiteSpace(Title)
        ? $"{ProcessName} ({ClassName})"
        : $"{Title} - {ProcessName}";

    public static WindowListItemViewModel FromWindowInfo(WindowInfo window)
    {
        ArgumentNullException.ThrowIfNull(window);

        return new WindowListItemViewModel(window);
    }

    public void AssignTarget(
        string layoutId,
        string layoutName,
        string zoneId,
        string zoneDisplayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layoutId);
        ArgumentException.ThrowIfNullOrWhiteSpace(layoutName);
        ArgumentException.ThrowIfNullOrWhiteSpace(zoneId);
        ArgumentException.ThrowIfNullOrWhiteSpace(zoneDisplayName);

        assignedLayoutId = layoutId;
        assignedLayoutName = layoutName;
        assignedZoneId = zoneId;
        assignedZoneDisplayName = zoneDisplayName;
        OnPropertyChanged(nameof(HasAssignment));
        OnPropertyChanged(nameof(AssignedLayoutId));
        OnPropertyChanged(nameof(AssignedZoneId));
        OnPropertyChanged(nameof(AssignmentText));
    }

    private static string CreateTitlePreview(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return string.Empty;
        }

        var trimmed = title.Trim();
        return trimmed.Length <= MaxTitleLength
            ? trimmed
            : string.Concat(trimmed.AsSpan(0, MaxTitleLength), "...");
    }
}
