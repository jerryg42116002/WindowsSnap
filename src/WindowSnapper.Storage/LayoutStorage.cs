using System.Text.Json;
using WindowSnapper.Core.Results;
using WindowSnapper.Layouts;

namespace WindowSnapper.Storage;

/// <summary>
/// Reads and writes user-defined layout JSON files.
/// </summary>
public sealed class LayoutStorage
{
    private readonly StoragePaths paths;
    private readonly LayoutValidator validator;
    private readonly JsonSerializerOptions jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="LayoutStorage"/> class.
    /// </summary>
    public LayoutStorage(StoragePaths paths)
        : this(paths, new LayoutValidator())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LayoutStorage"/> class.
    /// </summary>
    public LayoutStorage(StoragePaths paths, LayoutValidator validator)
    {
        this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
        this.validator = validator ?? throw new ArgumentNullException(nameof(validator));
        jsonOptions = JsonStorageOptions.Create();
    }

    /// <summary>
    /// Loads every custom layout JSON file from the layouts directory.
    /// </summary>
    public async Task<Result<IReadOnlyList<LayoutDefinition>>> LoadLayoutsAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(paths.LayoutsDirectoryPath);

        var layouts = new List<LayoutDefinition>();
        foreach (var filePath in Directory.EnumerateFiles(paths.LayoutsDirectoryPath, "*.json"))
        {
            var result = await LoadLayoutAsync(filePath, cancellationToken).ConfigureAwait(false);
            if (result.IsFailure)
            {
                return Result<IReadOnlyList<LayoutDefinition>>.Failure(result.ErrorCode, result.ErrorMessage);
            }

            layouts.Add(result.Value);
        }

        return Result<IReadOnlyList<LayoutDefinition>>.Success(layouts);
    }

    /// <summary>
    /// Loads and validates one custom layout JSON file.
    /// </summary>
    public async Task<Result<LayoutDefinition>> LoadLayoutAsync(string layoutFilePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layoutFilePath);

        try
        {
            await using var stream = File.OpenRead(layoutFilePath);
            var layout = await JsonSerializer.DeserializeAsync<LayoutDefinition>(stream, jsonOptions, cancellationToken).ConfigureAwait(false);
            if (layout is null)
            {
                return Result<LayoutDefinition>.Failure(ResultErrorCode.InvalidArgument, "Layout file did not contain a layout definition.");
            }

            var validation = validator.Validate(layout);
            if (validation.IsFailure)
            {
                return Result<LayoutDefinition>.Failure(
                    validation.ErrorCode,
                    $"Layout file '{Path.GetFileName(layoutFilePath)}' is invalid: {validation.ErrorMessage}");
            }

            return Result<LayoutDefinition>.Success(layout);
        }
        catch (JsonException ex)
        {
            return Result<LayoutDefinition>.Failure(
                ResultErrorCode.InvalidArgument,
                $"Layout file '{Path.GetFileName(layoutFilePath)}' contains invalid JSON: {ex.Message}");
        }
        catch (IOException ex)
        {
            return Result<LayoutDefinition>.Failure(
                ResultErrorCode.PlatformCallFailed,
                $"Could not read layout file '{Path.GetFileName(layoutFilePath)}': {ex.Message}");
        }
    }

    /// <summary>
    /// Saves a custom layout JSON file atomically after validation.
    /// </summary>
    public async Task<Result> SaveLayoutAsync(LayoutDefinition layout, CancellationToken cancellationToken = default)
    {
        var validation = validator.Validate(layout);
        if (validation.IsFailure)
        {
            return Result.Failure(validation.ErrorCode, validation.ErrorMessage);
        }

        try
        {
            Directory.CreateDirectory(paths.LayoutsDirectoryPath);
            var filePath = Path.Combine(paths.LayoutsDirectoryPath, $"{layout.Id}.json");
            await AtomicJsonFile.WriteAsync(filePath, layout, jsonOptions, cancellationToken).ConfigureAwait(false);

            return Result.Success();
        }
        catch (IOException ex)
        {
            return Result.Failure(
                ResultErrorCode.PlatformCallFailed,
                $"Could not write layout file '{layout.Id}.json': {ex.Message}");
        }
    }
}
