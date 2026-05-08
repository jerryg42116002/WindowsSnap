namespace WindowSnapper.Core.Results;

/// <summary>
/// Represents the outcome of an operation that returns a value.
/// </summary>
/// <typeparam name="T">The success value type.</typeparam>
public sealed record Result<T>
{
    private readonly T? value;

    private Result(bool isSuccess, T? value, ResultErrorCode errorCode, string errorMessage)
    {
        IsSuccess = isSuccess;
        this.value = value;
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
    /// Gets the success value, or throws when the result is failed.
    /// </summary>
    public T Value => IsSuccess
        ? value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    /// <summary>
    /// Gets the structured error code.
    /// </summary>
    public ResultErrorCode ErrorCode { get; }

    /// <summary>
    /// Gets the user-safe or log-safe error message.
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// Creates a successful result containing a value.
    /// </summary>
    public static Result<T> Success(T value) => new(true, value, ResultErrorCode.None, string.Empty);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static Result<T> Failure(ResultErrorCode errorCode, string errorMessage)
    {
        if (errorCode == ResultErrorCode.None)
        {
            throw new ArgumentOutOfRangeException(nameof(errorCode), errorCode, "Failure results must include an error code.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        return new Result<T>(false, default, errorCode, errorMessage);
    }
}
