namespace WindowSnapper.Core.Results;

/// <summary>
/// Provides structured error categories for recoverable operations.
/// </summary>
public enum ResultErrorCode
{
    /// <summary>
    /// No error occurred.
    /// </summary>
    None = 0,

    /// <summary>
    /// The operation failed for an unknown reason.
    /// </summary>
    Unknown = 1,

    /// <summary>
    /// An input value was invalid.
    /// </summary>
    InvalidArgument = 2,

    /// <summary>
    /// The requested item was not found.
    /// </summary>
    NotFound = 3,

    /// <summary>
    /// The operation is not supported for the target item.
    /// </summary>
    NotSupported = 4,

    /// <summary>
    /// The current process does not have permission to complete the operation.
    /// </summary>
    PermissionDenied = 5,

    /// <summary>
    /// The target window should not be managed by WindowSnapper.
    /// </summary>
    WindowNotManageable = 6,

    /// <summary>
    /// A platform API call failed.
    /// </summary>
    PlatformCallFailed = 7
}
