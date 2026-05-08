using System.Windows.Input;

namespace WindowSnapper.App.ViewModels;

internal sealed class MainWindowViewModel : ViewModelBase
{
    private bool hotkeysPaused;
    private bool minimizeToTray;
    private string statusMessage = "WindowSnapper 正在运行";

    public bool HotkeysPaused
    {
        get => hotkeysPaused;
        set
        {
            if (SetProperty(ref hotkeysPaused, value))
            {
                OnPropertyChanged(nameof(HotkeyStatusText));
                OnPropertyChanged(nameof(ToggleHotkeysText));
            }
        }
    }

    public bool MinimizeToTray
    {
        get => minimizeToTray;
        set => SetProperty(ref minimizeToTray, value);
    }

    public string StatusMessage
    {
        get => statusMessage;
        set => SetProperty(ref statusMessage, value);
    }

    public string HotkeyStatusText => HotkeysPaused ? "快捷键已暂停" : "快捷键已启用";

    public string ToggleHotkeysText => HotkeysPaused ? "恢复快捷键" : "暂停快捷键";

    public required ICommand OpenSettingsCommand { get; init; }

    public required ICommand OpenWindowSelectorCommand { get; init; }

    public required ICommand OpenLayoutEditorCommand { get; init; }

    public required ICommand ToggleHotkeysCommand { get; init; }

    public required ICommand ExitCommand { get; init; }
}
