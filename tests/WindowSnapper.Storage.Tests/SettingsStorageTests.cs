namespace WindowSnapper.Storage.Tests;

public sealed class SettingsStorageTests
{
    [Fact]
    public async Task MissingConfigCreatesDefaultSettings()
    {
        using var temp = TemporaryStorage.Create();
        var paths = temp.CreatePaths();
        var storage = new SettingsStorage(paths);

        var result = await storage.LoadOrCreateAsync();

        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(paths.ConfigFilePath));
        Assert.Equal(ConfigMigration.CurrentVersion, result.Value.Version);
        Assert.Equal("system", result.Value.Theme);
        Assert.Equal("zh-CN", result.Value.Language);
        Assert.True(result.Value.MinimizeToTray);
        Assert.Equal(0.35, result.Value.OverlayOpacity);
        Assert.Contains("WindowSnapperOverlayWindow", result.Value.IgnoredWindowClasses);
    }

    [Fact]
    public async Task ValidConfigLoadsExistingValues()
    {
        using var temp = TemporaryStorage.Create();
        var paths = temp.CreatePaths();
        Directory.CreateDirectory(Path.GetDirectoryName(paths.ConfigFilePath)!);
        await File.WriteAllTextAsync(paths.ConfigFilePath, """
            {
              "version": 1,
              "theme": "dark",
              "language": "en-US",
              "startWithWindows": true,
              "minimizeToTray": false,
              "showOverlayPreview": false,
              "hotkeysPaused": true,
              "overlayOpacity": 0.5,
              "defaultGap": 4,
              "defaultMargin": 6,
              "ignoredProcesses": [ "sample.exe" ],
              "ignoredWindowClasses": [ "SampleWindow" ]
            }
            """);
        var storage = new SettingsStorage(paths);

        var result = await storage.LoadOrCreateAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("dark", result.Value.Theme);
        Assert.Equal("en-US", result.Value.Language);
        Assert.True(result.Value.StartWithWindows);
        Assert.False(result.Value.MinimizeToTray);
        Assert.True(result.Value.HotkeysPaused);
        Assert.Equal(0.5, result.Value.OverlayOpacity);
        Assert.Equal(4, result.Value.DefaultGap);
        Assert.Equal("sample.exe", Assert.Single(result.Value.IgnoredProcesses));
        Assert.Contains("SampleWindow", result.Value.IgnoredWindowClasses);
        Assert.Contains("WindowSnapperOverlayWindow", result.Value.IgnoredWindowClasses);
    }

    [Fact]
    public async Task CorruptConfigIsBackedUpAndDefaultSettingsAreRestored()
    {
        using var temp = TemporaryStorage.Create();
        var paths = temp.CreatePaths();
        Directory.CreateDirectory(Path.GetDirectoryName(paths.ConfigFilePath)!);
        await File.WriteAllTextAsync(paths.ConfigFilePath, "{ invalid json");
        var storage = new SettingsStorage(paths);

        var result = await storage.LoadOrCreateAsync();

        Assert.True(result.IsSuccess);
        Assert.True(File.Exists($"{paths.ConfigFilePath}.bak"));
        Assert.Equal("system", result.Value.Theme);
        Assert.Contains("\"theme\": \"system\"", await File.ReadAllTextAsync(paths.ConfigFilePath), StringComparison.Ordinal);
    }

    [Fact]
    public async Task MissingNewFieldsUseDefaults()
    {
        using var temp = TemporaryStorage.Create();
        var paths = temp.CreatePaths();
        Directory.CreateDirectory(Path.GetDirectoryName(paths.ConfigFilePath)!);
        await File.WriteAllTextAsync(paths.ConfigFilePath, """
            {
              "version": 1,
              "theme": "dark"
            }
            """);
        var storage = new SettingsStorage(paths);

        var result = await storage.LoadOrCreateAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("dark", result.Value.Theme);
        Assert.Equal("zh-CN", result.Value.Language);
        Assert.True(result.Value.MinimizeToTray);
        Assert.True(result.Value.ShowOverlayPreview);
        Assert.False(result.Value.HotkeysPaused);
        Assert.Equal(0.35, result.Value.OverlayOpacity);
        Assert.Equal(8, result.Value.DefaultGap);
        Assert.Contains("Shell_TrayWnd", result.Value.IgnoredWindowClasses);
        Assert.Contains("WindowSnapperOverlayWindow", result.Value.IgnoredWindowClasses);
    }

    [Fact]
    public async Task OlderConfigVersionIsMigrated()
    {
        using var temp = TemporaryStorage.Create();
        var paths = temp.CreatePaths();
        Directory.CreateDirectory(Path.GetDirectoryName(paths.ConfigFilePath)!);
        await File.WriteAllTextAsync(paths.ConfigFilePath, """
            {
              "version": 0,
              "theme": "light",
              "language": "zh-CN"
            }
            """);
        var storage = new SettingsStorage(paths);

        var result = await storage.LoadOrCreateAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(ConfigMigration.CurrentVersion, result.Value.Version);
        Assert.Equal("light", result.Value.Theme);
        Assert.Contains($"\"version\": {ConfigMigration.CurrentVersion}", await File.ReadAllTextAsync(paths.ConfigFilePath), StringComparison.Ordinal);
    }
}
