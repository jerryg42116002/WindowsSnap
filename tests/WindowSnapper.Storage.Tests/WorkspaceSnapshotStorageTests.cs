using WindowSnapper.Core.Results;
using WindowSnapper.Core.Workspaces;

namespace WindowSnapper.Storage.Tests;

public sealed class WorkspaceSnapshotStorageTests
{
    [Fact]
    public async Task SaveAndLoadWorkspaceSnapshot()
    {
        using var temp = TemporaryStorage.Create();
        var storage = new WorkspaceSnapshotStorage(temp.CreatePaths());
        var snapshot = CreateSnapshot("workspace-1", new DateTimeOffset(2026, 5, 7, 1, 0, 0, TimeSpan.Zero));

        var save = await storage.SaveAsync(snapshot);
        var load = await storage.LoadAsync(snapshot.Id);

        Assert.True(save.IsSuccess);
        Assert.True(load.IsSuccess);
        Assert.Equal(snapshot.Id, load.Value.Id);
        Assert.Equal("notepad", Assert.Single(load.Value.Windows).ProcessName);
    }

    [Fact]
    public async Task LoadLatestReturnsNewestSnapshot()
    {
        using var temp = TemporaryStorage.Create();
        var storage = new WorkspaceSnapshotStorage(temp.CreatePaths());
        await storage.SaveAsync(CreateSnapshot("older", new DateTimeOffset(2026, 5, 7, 1, 0, 0, TimeSpan.Zero)));
        await storage.SaveAsync(CreateSnapshot("newer", new DateTimeOffset(2026, 5, 7, 2, 0, 0, TimeSpan.Zero)));

        var latest = await storage.LoadLatestAsync();

        Assert.True(latest.IsSuccess);
        Assert.Equal("newer", latest.Value.Id);
    }

    [Fact]
    public async Task ListSkipsCorruptSnapshotFiles()
    {
        using var temp = TemporaryStorage.Create();
        var paths = temp.CreatePaths();
        var storage = new WorkspaceSnapshotStorage(paths);
        await storage.SaveAsync(CreateSnapshot("valid", new DateTimeOffset(2026, 5, 7, 1, 0, 0, TimeSpan.Zero)));
        Directory.CreateDirectory(paths.WorkspacesDirectoryPath);
        await File.WriteAllTextAsync(Path.Combine(paths.WorkspacesDirectoryPath, "bad.json"), "{ invalid json");

        var list = await storage.ListAsync();

        Assert.True(list.IsSuccess);
        Assert.Equal("valid", Assert.Single(list.Value).Id);
    }

    [Fact]
    public async Task LoadAsyncReturnsClearErrorForCorruptSnapshotFile()
    {
        using var temp = TemporaryStorage.Create();
        var paths = temp.CreatePaths();
        Directory.CreateDirectory(paths.WorkspacesDirectoryPath);
        await File.WriteAllTextAsync(Path.Combine(paths.WorkspacesDirectoryPath, "bad.json"), "{ invalid json");
        var storage = new WorkspaceSnapshotStorage(paths);

        var result = await storage.LoadAsync("bad");

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorCode.InvalidArgument, result.ErrorCode);
        Assert.Contains("bad.json", result.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadAsyncReturnsClearErrorForInvalidSnapshotShape()
    {
        using var temp = TemporaryStorage.Create();
        var paths = temp.CreatePaths();
        Directory.CreateDirectory(paths.WorkspacesDirectoryPath);
        await File.WriteAllTextAsync(Path.Combine(paths.WorkspacesDirectoryPath, "invalid.json"), """
            {
              "version": 1,
              "id": "",
              "name": "Invalid",
              "createdAt": "2026-05-07T01:00:00Z",
              "windows": []
            }
            """);
        var storage = new WorkspaceSnapshotStorage(paths);

        var result = await storage.LoadAsync("invalid");

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultErrorCode.InvalidArgument, result.ErrorCode);
        Assert.Contains("invalid.json", result.ErrorMessage, StringComparison.Ordinal);
    }

    private static WorkspaceSnapshot CreateSnapshot(string id, DateTimeOffset createdAt)
    {
        return new WorkspaceSnapshot(
            1,
            id,
            $"Workspace {id}",
            createdAt,
            [
                new WorkspaceWindowSnapshot(
                    "notepad",
                    "Notepad",
                    @"\\.\DISPLAY1",
                    new RelativeRect(0, 0, 0.5, 1),
                    WorkspaceWindowState.Normal)
            ]);
    }
}
