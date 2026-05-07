using System.Diagnostics;
using WindowSnapper.Core.Results;
using WindowSnapper.Storage;

namespace WindowSnapper.App.ViewModels;

internal sealed class SettingsViewModel : ViewModelBase
{
    private readonly Func<AppSettings, Task<Result>> saveSettingsAsync;
    private readonly Func<bool, AppSettings, Task<Result>> setHotkeysPausedAsync;
    private AppSettings settings;
    private string ignoredProcessesText;
    private string ignoredWindowClassesText;
    private string errorMessage = string.Empty;
    private bool suppressSave;

    public SettingsViewModel(
        AppSettings settings,
        Func<AppSettings, Task<Result>> saveSettingsAsync,
        Func<bool, AppSettings, Task<Result>> setHotkeysPausedAsync)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.saveSettingsAsync = saveSettingsAsync ?? throw new ArgumentNullException(nameof(saveSettingsAsync));
        this.setHotkeysPausedAsync = setHotkeysPausedAsync ?? throw new ArgumentNullException(nameof(setHotkeysPausedAsync));
        ignoredProcessesText = JoinLines(settings.IgnoredProcesses);
        ignoredWindowClassesText = JoinLines(settings.IgnoredWindowClasses);
    }

    public bool MinimizeToTray
    {
        get => settings.MinimizeToTray;
        set => UpdateSettings(settings with { MinimizeToTray = value }, nameof(MinimizeToTray));
    }

    public bool ShowOverlayPreview
    {
        get => settings.ShowOverlayPreview;
        set => UpdateSettings(settings with { ShowOverlayPreview = value }, nameof(ShowOverlayPreview));
    }

    public bool HotkeysPaused
    {
        get => settings.HotkeysPaused;
        set => UpdateHotkeysPaused(value);
    }

    public int DefaultGap
    {
        get => settings.DefaultGap;
        set => UpdateSettings(settings with { DefaultGap = Math.Max(0, value) }, nameof(DefaultGap));
    }

    public int DefaultMargin
    {
        get => settings.DefaultMargin;
        set => UpdateSettings(settings with { DefaultMargin = Math.Max(0, value) }, nameof(DefaultMargin));
    }

    public string IgnoredProcessesText
    {
        get => ignoredProcessesText;
        set
        {
            if (SetProperty(ref ignoredProcessesText, value))
            {
                UpdateSettings(settings with { IgnoredProcesses = SplitLines(value) }, savePropertyChanged: false);
            }
        }
    }

    public string IgnoredWindowClassesText
    {
        get => ignoredWindowClassesText;
        set
        {
            if (SetProperty(ref ignoredWindowClassesText, value))
            {
                UpdateSettings(settings with { IgnoredWindowClasses = SplitLines(value) }, savePropertyChanged: false);
            }
        }
    }

    public string ErrorMessage
    {
        get => errorMessage;
        private set => SetProperty(ref errorMessage, value);
    }

    public void Refresh(AppSettings currentSettings)
    {
        ArgumentNullException.ThrowIfNull(currentSettings);

        suppressSave = true;
        settings = currentSettings;
        ignoredProcessesText = JoinLines(settings.IgnoredProcesses);
        ignoredWindowClassesText = JoinLines(settings.IgnoredWindowClasses);
        suppressSave = false;

        OnPropertyChanged(nameof(MinimizeToTray));
        OnPropertyChanged(nameof(ShowOverlayPreview));
        OnPropertyChanged(nameof(HotkeysPaused));
        OnPropertyChanged(nameof(DefaultGap));
        OnPropertyChanged(nameof(DefaultMargin));
        OnPropertyChanged(nameof(IgnoredProcessesText));
        OnPropertyChanged(nameof(IgnoredWindowClassesText));
    }

    private void UpdateSettings(AppSettings updatedSettings, string? propertyName = null, bool savePropertyChanged = true)
    {
        if (suppressSave || updatedSettings == settings)
        {
            return;
        }

        settings = updatedSettings;
        if (savePropertyChanged && propertyName is not null)
        {
            OnPropertyChanged(propertyName);
        }

        QueueSave(settings);
    }

    private void UpdateHotkeysPaused(bool paused)
    {
        if (settings.HotkeysPaused == paused)
        {
            return;
        }

        settings = settings with { HotkeysPaused = paused };
        OnPropertyChanged(nameof(HotkeysPaused));
        QueueHotkeyPause(settings);
    }

    private void QueueSave(AppSettings snapshot)
    {
        _ = SaveAsync(snapshot);
    }

    private void QueueHotkeyPause(AppSettings snapshot)
    {
        _ = SetHotkeysPausedAsync(snapshot);
    }

    private async Task SaveAsync(AppSettings snapshot)
    {
        try
        {
            var result = await saveSettingsAsync(snapshot);
            ErrorMessage = result.IsSuccess ? string.Empty : "设置保存失败，请稍后重试。";
        }
        catch (Exception ex)
        {
            Trace.TraceError("Settings save failed: {0}", ex.Message);
            ErrorMessage = "设置保存失败，请稍后重试。";
        }
    }

    private async Task SetHotkeysPausedAsync(AppSettings snapshot)
    {
        try
        {
            var result = await setHotkeysPausedAsync(snapshot.HotkeysPaused, snapshot);
            ErrorMessage = result.IsSuccess ? string.Empty : result.ErrorMessage;
        }
        catch (Exception ex)
        {
            Trace.TraceError("Hotkey pause update failed: {0}", ex.Message);
            ErrorMessage = "快捷键状态更新失败，请稍后重试。";
        }
    }

    private static string JoinLines(IReadOnlyList<string> values)
    {
        return string.Join(Environment.NewLine, values);
    }

    private static IReadOnlyList<string> SplitLines(string value)
    {
        return value
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
