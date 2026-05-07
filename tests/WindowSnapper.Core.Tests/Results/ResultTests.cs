using WindowSnapper.Core.Results;

namespace WindowSnapper.Core.Tests.Results;

public sealed class ResultTests
{
    [Fact]
    public void SuccessHasSuccessfulState()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(ResultErrorCode.None, result.ErrorCode);
        Assert.Equal(string.Empty, result.ErrorMessage);
    }

    [Fact]
    public void FailureHasErrorState()
    {
        var result = Result.Failure(ResultErrorCode.WindowNotManageable, "Window cannot be managed.");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorCode.WindowNotManageable, result.ErrorCode);
        Assert.Equal("Window cannot be managed.", result.ErrorMessage);
    }

    [Fact]
    public void GenericSuccessContainsValue()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Equal(ResultErrorCode.None, result.ErrorCode);
    }

    [Fact]
    public void GenericFailureHasErrorState()
    {
        var result = Result<int>.Failure(ResultErrorCode.NotFound, "Window was not found.");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorCode.NotFound, result.ErrorCode);
        Assert.Equal("Window was not found.", result.ErrorMessage);
    }

    [Fact]
    public void GenericFailureDoesNotExposeValue()
    {
        var result = Result<int>.Failure(ResultErrorCode.Unknown, "Unknown failure.");

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }
}
