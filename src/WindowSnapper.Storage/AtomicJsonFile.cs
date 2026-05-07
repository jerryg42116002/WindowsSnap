using System.Text.Json;

namespace WindowSnapper.Storage;

internal static class AtomicJsonFile
{
    public static async Task WriteAsync<T>(
        string targetPath,
        T value,
        JsonSerializerOptions jsonOptions,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = $"{targetPath}.tmp";
        await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(stream, value, jsonOptions, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        File.Move(tempPath, targetPath, overwrite: true);
    }
}
