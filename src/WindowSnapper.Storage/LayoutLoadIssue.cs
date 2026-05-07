using WindowSnapper.Core.Results;

namespace WindowSnapper.Storage;

/// <summary>
/// Describes a non-fatal layout file load issue.
/// </summary>
/// <param name="FileName">The safe file name that failed.</param>
/// <param name="ErrorCode">The structured error code.</param>
/// <param name="Message">A user-safe diagnostic message.</param>
public sealed record LayoutLoadIssue(string FileName, ResultErrorCode ErrorCode, string Message);
