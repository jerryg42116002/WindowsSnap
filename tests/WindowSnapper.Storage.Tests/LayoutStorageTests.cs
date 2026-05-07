using WindowSnapper.Core.Results;

namespace WindowSnapper.Storage.Tests;

public sealed class LayoutStorageTests
{
    [Fact]
    public async Task LoadsValidLayoutJson()
    {
        using var temp = TemporaryStorage.Create();
        var paths = temp.CreatePaths();
        Directory.CreateDirectory(paths.LayoutsDirectoryPath);
        var layoutPath = Path.Combine(paths.LayoutsDirectoryPath, "dev-layout.json");
        await File.WriteAllTextAsync(layoutPath, """
            {
              "id": "dev-layout",
              "name": "Dev Layout",
              "version": 1,
              "gap": 8,
              "margin": 8,
              "zones": [
                {
                  "id": "code",
                  "name": "Code",
                  "x": 0,
                  "y": 0,
                  "width": 0.6,
                  "height": 1
                }
              ]
            }
            """);

        var storage = new LayoutStorage(paths);

        var result = await storage.LoadLayoutsAsync();

        Assert.True(result.IsSuccess);
        var layout = Assert.Single(result.Value);
        Assert.Equal("dev-layout", layout.Id);
        Assert.Equal("code", Assert.Single(layout.Zones).Id);
    }

    [Fact]
    public async Task InvalidLayoutJsonReturnsClearError()
    {
        using var temp = TemporaryStorage.Create();
        var paths = temp.CreatePaths();
        Directory.CreateDirectory(paths.LayoutsDirectoryPath);
        var layoutPath = Path.Combine(paths.LayoutsDirectoryPath, "invalid-layout.json");
        await File.WriteAllTextAsync(layoutPath, """
            {
              "id": "invalid-layout",
              "name": "Invalid Layout",
              "version": 1,
              "gap": 0,
              "margin": 0,
              "zones": [
                {
                  "id": "bad",
                  "name": "Bad",
                  "x": 0.8,
                  "y": 0,
                  "width": 0.3,
                  "height": 1
                }
              ]
            }
            """);

        var storage = new LayoutStorage(paths);

        var result = await storage.LoadLayoutAsync(layoutPath);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorCode.InvalidArgument, result.ErrorCode);
        Assert.Contains("invalid-layout.json", result.ErrorMessage, StringComparison.Ordinal);
        Assert.Contains("x + width", result.ErrorMessage, StringComparison.Ordinal);
    }
}
