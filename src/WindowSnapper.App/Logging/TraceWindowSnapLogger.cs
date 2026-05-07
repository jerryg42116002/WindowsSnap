using System.Diagnostics;
using WindowSnapper.Core.Results;
using WindowSnapper.Snap;

namespace WindowSnapper.App.Logging;

internal sealed class TraceWindowSnapLogger : IWindowSnapLogger
{
    public void SnapStarted(SnapCommand command)
    {
        Trace.TraceInformation(
            "Snap started. LayoutId={0}; ZoneId={1}",
            command.LayoutId,
            command.ZoneId);
    }

    public void SnapSucceeded(SnapCommand command)
    {
        Trace.TraceInformation(
            "Snap succeeded. LayoutId={0}; ZoneId={1}",
            command.LayoutId,
            command.ZoneId);
    }

    public void SnapFailed(SnapCommand command, ResultErrorCode errorCode, string diagnosticMessage)
    {
        Trace.TraceWarning(
            "Snap failed. LayoutId={0}; ZoneId={1}; ErrorCode={2}; Message={3}",
            command.LayoutId,
            command.ZoneId,
            errorCode,
            diagnosticMessage);
    }
}
