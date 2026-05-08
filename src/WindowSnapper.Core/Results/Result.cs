namespace WindowSnapper.Core.Results;

/// <summary>
/// Represents the outcome of an operation that does not return a value.
/// </summary>
public sealed record Result
{
    private Result(bool isSuccess, ResultErrorCode errorCode, string errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Gets whether the operation completed successfully.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the structured error code.
    /// </summary>
    public ResultErrorCode ErrorCode { get; }

    /// <summary>
    /// Gets the user-safe or log-safe error message.
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new(true, ResultErrorCode.None, string.Empty);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static Result Failure(ResultErrorCode errorCode, string errorMessage)
    {
        if (errorCode == ResultErrorCode.None)
        {
            throw new ArgumentOutOfRangeException(nameof(errorCode), errorCode, "Failure results must include an error code.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        return new Result(false, errorCode, errorMessage);
    }
}
