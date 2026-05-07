namespace WindowSnapper.Storage.Tests;

internal sealed class TemporaryStorage : IDisposable
{
    private TemporaryStorage(string rootPath)
    {
        RootPath = rootPath;
    }

    public string RootPath { get; }

    public static TemporaryStorage Create()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "WindowSnapper.Storage.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootPath);

        return new TemporaryStorage(rootPath);
    }

    public StoragePaths CreatePaths()
    {
        return new StoragePaths(
            Path.Combine(RootPath, "config.json"),
            Path.Combine(RootPath, "layouts"),
            Path.Combine(RootPath, "logs", "app.log"));
    }

    public void Dispose()
    {
        if (Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, recursive: true);
        }
    }
}
