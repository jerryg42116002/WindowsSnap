using System.Text.Json;
using WindowSnapper.Core.Results;

namespace WindowSnapper.Storage;

/// <summary>
/// Reads and writes application settings.
/// </summary>
public sealed class SettingsStorage
{
    private readonly StoragePaths paths;
    private readonly DefaultSettingsFactory defaultSettingsFactory;
    private readonly ConfigMigration configMigration;
    private readonly JsonSerializerOptions jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsStorage"/> class.
    /// </summary>
    public SettingsStorage(StoragePaths paths)
        : this(paths, new DefaultSettingsFactory())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsStorage"/> class.
    /// </summary>
    public SettingsStorage(StoragePaths paths, DefaultSettingsFactory defaultSettingsFactory)
    {
        this.paths = paths ?? throw new ArgumentNullException(nameof(paths));
        this.defaultSettingsFactory = defaultSettingsFactory ?? throw new ArgumentNullException(nameof(defaultSettingsFactory));
        configMigration = new ConfigMigration(defaultSettingsFactory);
        jsonOptions = JsonStorageOptions.Create();
    }

    /// <summary>
    /// Loads settings, creating defaults when the configuration file is missing or corrupt.
    /// </summary>
    public async Task<Result<AppSettings>> LoadOrCreateAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(paths.ConfigFilePath))
        {
            var defaults = defaultSettingsFactory.Create();
            var saveResult = await SaveAsync(defaults, cancellationToken).ConfigureAwait(false);
            if (saveResult.IsFailure)
            {
                return Result<AppSettings>.Failure(saveResult.ErrorCode, saveResult.ErrorMessage);
            }

            return Result<AppSettings>.Success(defaults);
        }

        try
        {
            await using var stream = File.OpenRead(paths.ConfigFilePath);
            var loaded = await JsonSerializer.DeserializeAsync<AppSettings>(stream, jsonOptions, cancellationToken).ConfigureAwait(false);
            var migrated = configMigration.Migrate(loaded);
            if (loaded is null || loaded.Version != migrated.Version)
            {
                await SaveAsync(migrated, cancellationToken).ConfigureAwait(false);
            }

            return Result<AppSettings>.Success(migrated);
        }
        catch (JsonException)
        {
            return await BackupCorruptConfigAndRestoreDefaultAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            return await BackupCorruptConfigAndRestoreDefaultAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (IOException ex)
        {
            return Result<AppSettings>.Failure(
                ResultErrorCode.PlatformCallFailed,
                $"Could not read settings file: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Result<AppSettings>.Failure(
                ResultErrorCode.PermissionDenied,
                $"Could not access settings file: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves settings with an atomic write.
    /// </summary>
    public async Task<Result> SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            var migrated = configMigration.Migrate(settings);
            Directory.CreateDirectory(Path.GetDirectoryName(paths.ConfigFilePath)!);
            await AtomicJsonFile.WriteAsync(paths.ConfigFilePath, migrated, jsonOptions, cancellationToken).ConfigureAwait(false);

            return Result.Success();
        }
        catch (IOException ex)
        {
            return Result.Failure(
                ResultErrorCode.PlatformCallFailed,
                $"Could not write settings file: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Result.Failure(
                ResultErrorCode.PermissionDenied,
                $"Could not write settings file: {ex.Message}");
        }
    }

    private async Task<Result<AppSettings>> BackupCorruptConfigAndRestoreDefaultAsync(CancellationToken cancellationToken)
    {
        try
        {
            var backupPath = $"{paths.ConfigFilePath}.bak";
            Directory.CreateDirectory(Path.GetDirectoryName(paths.ConfigFilePath)!);
            File.Copy(paths.ConfigFilePath, backupPath, overwrite: true);

            var defaults = defaultSettingsFactory.Create();
            var saveResult = await SaveAsync(defaults, cancellationToken).ConfigureAwait(false);
            if (saveResult.IsFailure)
            {
                return Result<AppSettings>.Failure(saveResult.ErrorCode, saveResult.ErrorMessage);
            }

            return Result<AppSettings>.Success(defaults);
        }
        catch (IOException ex)
        {
            return Result<AppSettings>.Failure(
                ResultErrorCode.PlatformCallFailed,
                $"Could not recover settings file: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Result<AppSettings>.Failure(
                ResultErrorCode.PermissionDenied,
                $"Could not recover settings file: {ex.Message}");
        }
    }
}
