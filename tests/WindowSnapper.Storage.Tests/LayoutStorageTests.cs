using WindowSnapper.Core.Results;

namespace WindowSnapper.Storage.Tests;

public sealed class LayoutStorageTests
{
    [Fact]
    public async Task LoadsSingleValidLayoutJson()
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
        var layout = Assert.Single(result.Value.Layouts).Layout;
        Assert.Equal("dev-layout", layout.Id);
        Assert.Equal("code", Assert.Single(layout.Zones).Id);
        Assert.Empty(result.Value.Issues);
    }

    [Fact]
    public async Task LoadsMultipleValidLayoutJsonFiles()
    {
        using var temp = TemporaryStorage.Create();
        var paths = temp.CreatePaths();
        Directory.CreateDirectory(paths.LayoutsDirectoryPath);
        await WriteLayoutAsync(paths, "dev-layout", "Dev Layout");
        await WriteLayoutAsync(paths, "research-layout", "Research Layout");

        var storage = new LayoutStorage(paths);

        var result = await storage.LoadLayoutsAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(
            new[] { "dev-layout", "research-layout" },
            result.Value.Layouts.Select(layout => layout.Layout.Id).ToArray());
        Assert.Empty(result.Value.Issues);
    }

    [Fact]
    public async Task InvalidLayoutIsSkippedAndReturnedAsIssue()
    {
        using var temp = TemporaryStorage.Create();
        var paths = temp.CreatePaths();
        Directory.CreateDirectory(paths.LayoutsDirectoryPath);
        await WriteLayoutAsync(paths, "valid-layout", "Valid Layout");
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

        var result = await storage.LoadLayoutsAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("valid-layout", Assert.Single(result.Value.Layouts).Layout.Id);
        var issue = Assert.Single(result.Value.Issues);
        Assert.Equal(ResultErrorCode.InvalidArgument, issue.ErrorCode);
        Assert.Equal("invalid-layout.json", issue.FileName);
        Assert.Contains("x + width", issue.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EmptyLayoutsDirectoryReturnsNoUserLayouts()
    {
        using var temp = TemporaryStorage.Create();
        var paths = temp.CreatePaths();
        var storage = new LayoutStorage(paths);

        var result = await storage.LoadLayoutsAsync();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Layouts);
        Assert.Empty(result.Value.Issues);
    }

    [Fact]
    public async Task LoadLayoutAsyncReturnsClearErrorForInvalidSingleFile()
    {
        using var temp = TemporaryStorage.Create();
        var paths = temp.CreatePaths();
        Directory.CreateDirectory(paths.LayoutsDirectoryPath);
        var layoutPath = Path.Combine(paths.LayoutsDirectoryPath, "invalid-layout.json");
        await File.WriteAllTextAsync(layoutPath, "{ invalid json");

        var storage = new LayoutStorage(paths);

        var result = await storage.LoadLayoutAsync(layoutPath);

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorCode.InvalidArgument, result.ErrorCode);
        Assert.Contains("invalid-layout.json", result.ErrorMessage, StringComparison.Ordinal);
    }

    private static async Task WriteLayoutAsync(StoragePaths paths, string id, string name)
    {
        await File.WriteAllTextAsync(Path.Combine(paths.LayoutsDirectoryPath, $"{id}.json"), $$"""
            {
              "id": "{{id}}",
              "name": "{{name}}",
              "version": 1,
              "gap": 8,
              "margin": 8,
              "zones": [
                {
                  "id": "main",
                  "name": "Main",
                  "x": 0,
                  "y": 0,
                  "width": 1,
                  "height": 1
                }
              ]
            }
            """);
    }
}
