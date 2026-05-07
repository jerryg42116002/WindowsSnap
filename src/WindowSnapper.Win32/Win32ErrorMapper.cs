using System.ComponentModel;
using WindowSnapper.Core.Results;

namespace WindowSnapper.Win32;

/// <summary>
/// Maps Win32 error codes to Core result errors.
/// </summary>
public static class Win32ErrorMapper
{
    private const int ErrorAccessDenied = 5;
    private const int ErrorInvalidWindowHandle = 1400;

    /// <summary>
    /// Creates a failed result from a known Win32 error code.
    /// </summary>
    public static Result ToFailure(int errorCode, string operation)
    {
        return Result.Failure(MapErrorCode(errorCode), CreateMessage(errorCode, operation));
    }

    /// <summary>
    /// Creates a failed result from a known Win32 error code.
    /// </summary>
    public static Result<T> ToFailure<T>(int errorCode, string operation)
    {
        return Result<T>.Failure(MapErrorCode(errorCode), CreateMessage(errorCode, operation));
    }

    /// <summary>
    /// Maps a numeric Win32 error code to a Core result error code.
    /// </summary>
    public static ResultErrorCode MapErrorCode(int errorCode)
    {
        return errorCode switch
        {
            ErrorAccessDenied => ResultErrorCode.PermissionDenied,
            ErrorInvalidWindowHandle => ResultErrorCode.NotFound,
            _ => ResultErrorCode.PlatformCallFailed
        };
    }

    private static string CreateMessage(int errorCode, string operation)
    {
        var message = errorCode == 0
            ? "The platform call failed without an error code."
            : new Win32Exception(errorCode).Message;

        return $"{operation} failed with Win32 error {errorCode}: {message}";
    }
}
