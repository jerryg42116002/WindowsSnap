using System.Text.Json;

namespace WindowSnapper.Storage;

internal static class JsonStorageOptions
{
    public static JsonSerializerOptions Create()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }
}
