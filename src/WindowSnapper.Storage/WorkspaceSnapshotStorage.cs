using System.Text.Json;
using WindowSnapper.Core.Results;
using WindowSnapper.Core.Workspaces;

namespace WindowSnapper.Storage;

/// <summary>
/// Reads and writes workspace snapshots from local JSON files.
/// </summary>
public sealed class WorkspaceSnapshotStorage : IWorkspaceSnapshotStore
{
    private readonly StoragePaths paths;
    private readonly JsonSerializerOptions jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspaceSnapshotStorage"/> class.
    /// </summary>
    public WorkspaceSnapshotStorage(StoragePaths paths)
    {
        this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
        jsonOptions = JsonStorageOptions.Create();
    }

    /// <inheritdoc />
    public async Task<Result> SaveAsync(WorkspaceSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var idValidation = ValidateSnapshotId(snapshot.Id);
        if (idValidation.IsFailure)
        {
            return idValidation;
        }

        var snapshotValidation = ValidateSnapshot(snapshot);
        if (snapshotValidation.IsFailure)
        {
            return snapshotValidation;
        }

        try
        {
            Directory.CreateDirectory(paths.WorkspacesDirectoryPath);
            var filePath = GetSnapshotPath(snapshot.Id);
            await AtomicJsonFile.WriteAsync(filePath, snapshot, jsonOptions, cancellationToken).ConfigureAwait(false);

            return Result.Success();
        }
        catch (IOException ex)
        {
            return Result.Failure(
                ResultErrorCode.PlatformCallFailed,
                $"Could not write workspace snapshot '{snapshot.Id}.json': {ex.Message}");
        }
        catch (UnauthorizedAccessException)
        {
            return Result.Failure(
                ResultErrorCode.PermissionDenied,
                $"Could not write workspace snapshot '{snapshot.Id}.json'.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<WorkspaceSnapshot>> LoadAsync(string id, CancellationToken cancellationToken = default)
    {
        var idValidation = ValidateSnapshotId(id);
        if (idValidation.IsFailure)
        {
            return Result<WorkspaceSnapshot>.Failure(idValidation.ErrorCode, idValidation.ErrorMessage);
        }

        var filePath = GetSnapshotPath(id);
        if (!File.Exists(filePath))
        {
            return Result<WorkspaceSnapshot>.Failure(
                ResultErrorCode.NotFound,
                $"Workspace snapshot '{id}.json' was not found.");
        }

        return await LoadSnapshotFileAsync(filePath, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<WorkspaceSnapshot>> LoadLatestAsync(CancellationToken cancellationToken = default)
    {
        var list = await ListAsync(cancellationToken).ConfigureAwait(false);
        if (list.IsFailure)
        {
            return Result<WorkspaceSnapshot>.Failure(list.ErrorCode, list.ErrorMessage);
        }

        var latest = list.Value
            .OrderByDescending(snapshot => snapshot.CreatedAt)
            .FirstOrDefault();

        return latest is null
            ? Result<WorkspaceSnapshot>.Failure(ResultErrorCode.NotFound, "No workspace snapshots were found.")
            : Result<WorkspaceSnapshot>.Success(latest);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<WorkspaceSnapshot>>> ListAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(paths.WorkspacesDirectoryPath);
            var snapshots = new List<WorkspaceSnapshot>();
            foreach (var filePath in Directory
                .EnumerateFiles(paths.WorkspacesDirectoryPath, "*.json")
                .OrderBy(filePath => filePath, StringComparer.OrdinalIgnoreCase))
            {
                var snapshot = await LoadSnapshotFileAsync(filePath, cancellationToken).ConfigureAwait(false);
                if (snapshot.IsSuccess)
                {
                    snapshots.Add(snapshot.Value);
                }
            }

            return Result<IReadOnlyList<WorkspaceSnapshot>>.Success(snapshots);
        }
        catch (IOException)
        {
            return Result<IReadOnlyList<WorkspaceSnapshot>>.Failure(
                ResultErrorCode.PlatformCallFailed,
                "Could not enumerate workspace snapshot files.");
        }
        catch (UnauthorizedAccessException)
        {
            return Result<IReadOnlyList<WorkspaceSnapshot>>.Failure(
                ResultErrorCode.PermissionDenied,
                "Could not access the workspace snapshot directory.");
        }
    }

    private async Task<Result<WorkspaceSnapshot>> LoadSnapshotFileAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(filePath);
        try
        {
            await using var stream = File.OpenRead(filePath);
            var snapshot = await JsonSerializer.DeserializeAsync<WorkspaceSnapshot>(
                stream,
                jsonOptions,
                cancellationToken).ConfigureAwait(false);

            return snapshot is null
                ? Result<WorkspaceSnapshot>.Failure(
                    ResultErrorCode.InvalidArgument,
                    $"Workspace snapshot file '{fileName}' did not contain a snapshot.")
                : ValidateLoadedSnapshot(snapshot, fileName);
        }
        catch (JsonException ex)
        {
            return Result<WorkspaceSnapshot>.Failure(
                ResultErrorCode.InvalidArgument,
                $"Workspace snapshot file '{fileName}' contains invalid JSON: {ex.Message}");
        }
        catch (IOException)
        {
            return Result<WorkspaceSnapshot>.Failure(
                ResultErrorCode.PlatformCallFailed,
                $"Could not read workspace snapshot file '{fileName}'.");
        }
        catch (UnauthorizedAccessException)
        {
            return Result<WorkspaceSnapshot>.Failure(
                ResultErrorCode.PermissionDenied,
                $"Could not access workspace snapshot file '{fileName}'.");
        }
    }

    private string GetSnapshotPath(string id)
    {
        return Path.Combine(paths.WorkspacesDirectoryPath, $"{id}.json");
    }

    private static Result ValidateSnapshotId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Result.Failure(ResultErrorCode.InvalidArgument, "Workspace snapshot id is required.");
        }

        if (id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            id.Contains(Path.DirectorySeparatorChar) ||
            id.Contains(Path.AltDirectorySeparatorChar))
        {
            return Result.Failure(ResultErrorCode.InvalidArgument, "Workspace snapshot id contains invalid file name characters.");
        }

        return Result.Success();
    }

    private static Result<WorkspaceSnapshot> ValidateLoadedSnapshot(WorkspaceSnapshot snapshot, string fileName)
    {
        var validation = ValidateSnapshot(snapshot);
        return validation.IsFailure
            ? Result<WorkspaceSnapshot>.Failure(
                validation.ErrorCode,
                $"Workspace snapshot file '{fileName}' is invalid: {validation.ErrorMessage}")
            : Result<WorkspaceSnapshot>.Success(snapshot);
    }

    private static Result ValidateSnapshot(WorkspaceSnapshot snapshot)
    {
        if (snapshot.Version <= 0)
        {
            return Result.Failure(ResultErrorCode.InvalidArgument, "Workspace snapshot version must be greater than 0.");
        }

        var idValidation = ValidateSnapshotId(snapshot.Id);
        if (idValidation.IsFailure)
        {
            return idValidation;
        }

        if (string.IsNullOrWhiteSpace(snapshot.Name))
        {
            return Result.Failure(ResultErrorCode.InvalidArgument, "Workspace snapshot name is required.");
        }

        if (snapshot.Windows is null)
        {
            return Result.Failure(ResultErrorCode.InvalidArgument, "Workspace snapshot windows are required.");
        }

        foreach (var window in snapshot.Windows)
        {
            if (string.IsNullOrWhiteSpace(window.ProcessName) ||
                string.IsNullOrWhiteSpace(window.ClassName) ||
                string.IsNullOrWhiteSpace(window.MonitorDeviceName))
            {
                return Result.Failure(ResultErrorCode.InvalidArgument, "Workspace snapshot window identity fields are required.");
            }

            if (window.RelativeRect.Width <= 0 || window.RelativeRect.Height <= 0)
            {
                return Result.Failure(ResultErrorCode.InvalidArgument, "Workspace snapshot window relative size must be greater than 0.");
            }
        }

        return Result.Success();
    }
}
