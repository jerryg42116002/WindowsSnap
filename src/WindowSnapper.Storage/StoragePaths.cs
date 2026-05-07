namespace WindowSnapper.Storage;

/// <summary>
/// Provides all local paths used by storage services.
/// </summary>
/// <param name="ConfigFilePath">The application settings JSON file path.</param>
/// <param name="LayoutsDirectoryPath">The custom layouts directory path.</param>
/// <param name="LogFilePath">The local log file path.</param>
public sealed record StoragePaths(
    string ConfigFilePath,
    string LayoutsDirectoryPath,
    string LogFilePath)
{
    /// <summary>
    /// Creates paths using the platform application data locations.
    /// </summary>
    public static StoragePaths CreateDefault()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        var root = Path.Combine(appData, "WindowSnapper");
        var localRoot = Path.Combine(localAppData, "WindowSnapper");

        return new StoragePaths(
            Path.Combine(root, "config.json"),
            Path.Combine(root, "layouts"),
            Path.Combine(localRoot, "logs", "app.log"));
    }
}
