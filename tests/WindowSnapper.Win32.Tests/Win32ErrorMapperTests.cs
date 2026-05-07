using WindowSnapper.Core.Results;

namespace WindowSnapper.Win32.Tests;

public sealed class Win32ErrorMapperTests
{
    [Fact]
    public void MapsAccessDeniedToPermissionDenied()
    {
        var result = Win32ErrorMapper.ToFailure(5, "SetWindowPos");

        Assert.Equal(ResultErrorCode.PermissionDenied, result.ErrorCode);
        Assert.Contains("SetWindowPos", result.ErrorMessage, StringComparison.Ordinal);
        Assert.Contains("5", result.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void MapsInvalidWindowHandleToNotFound()
    {
        var result = Win32ErrorMapper.ToFailure(1400, "GetWindowRect");

        Assert.Equal(ResultErrorCode.NotFound, result.ErrorCode);
    }

    [Fact]
    public void MapsUnknownErrorToPlatformCallFailed()
    {
        var result = Win32ErrorMapper.ToFailure(12345, "MonitorFromWindow");

        Assert.Equal(ResultErrorCode.PlatformCallFailed, result.ErrorCode);
    }
}
