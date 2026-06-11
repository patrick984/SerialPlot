using System;
using SerialPlot.Models;

namespace SerialPlot.Services;

public readonly record struct XRange(double Minimum, double Maximum)
{
    public double Width => Maximum - Minimum;
}

public sealed class SteppedXAxisViewport
{
    public const int DefaultFutureSpaceSeconds = UserPreferences.DefaultSteppedFutureSpaceSeconds;
    public const double ExpansionThreshold = 0.9;

    private XRange? _target;

    public XRange? Current => _target;

    public void Reset() => _target = null;

    public XRange? Update(
        double oldestX,
        double newestX,
        XAutoscaleMode mode,
        XRange? visibleRange,
        double sampleRatePerSecond,
        double recentXSpacing,
        int futureSpaceSeconds = DefaultFutureSpaceSeconds)
    {
        if (!double.IsFinite(oldestX) || !double.IsFinite(newestX))
        {
            return _target;
        }

        if (newestX < oldestX)
        {
            (oldestX, newestX) = (newestX, oldestX);
        }

        var retainedRange = CreateDataRange(oldestX, newestX);
        var futureSpan = EstimateFutureSpan(sampleRatePerSecond, recentXSpacing, futureSpaceSeconds);
        var current = visibleRange is { Width: > 0 } range ? range : _target;
        if (current is not { Width: > 0 } visible)
        {
            _target = CreateInitialRange(retainedRange, futureSpan);
            return _target;
        }

        var retainedRangeOutsideVisibleRange = newestX < visible.Minimum || oldestX > visible.Maximum;
        if (!retainedRangeOutsideVisibleRange && newestX < visible.Minimum + (visible.Width * ExpansionThreshold))
        {
            return _target;
        }

        _target = mode is XAutoscaleMode.SteppedPan
            ? CreatePanRange(visible, newestX, futureSpan)
            : CreateExpansionRange(retainedRange, futureSpan);

        return _target;
    }

    private static XRange CreateDataRange(double oldestX, double newestX)
        => new(oldestX, newestX);

    private static XRange CreateExpansionRange(XRange retainedRange, double futureSpan)
        => new(retainedRange.Minimum, retainedRange.Maximum + futureSpan);

    private static XRange CreateInitialRange(XRange retainedRange, double futureSpan)
        => CreateExpansionRange(retainedRange, futureSpan);

    private static XRange CreatePanRange(XRange visibleRange, double newestX, double futureSpan)
    {
        var maximum = newestX + futureSpan;
        return new XRange(maximum - visibleRange.Width, maximum);
    }

    private static double EstimateFutureSpan(double sampleRatePerSecond, double recentXSpacing, int futureSpaceSeconds)
    {
        futureSpaceSeconds = UserPreferences.ClampSteppedFutureSpaceSeconds(futureSpaceSeconds);
        var span = sampleRatePerSecond > 0 && double.IsFinite(sampleRatePerSecond)
            && recentXSpacing > 0 && double.IsFinite(recentXSpacing)
            ? sampleRatePerSecond * futureSpaceSeconds * recentXSpacing
            : 1;

        return span > 0 && double.IsFinite(span) ? span : 1;
    }
}
