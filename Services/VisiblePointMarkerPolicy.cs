using System;
using System.Collections.Generic;

namespace SerialPlot.Services;

public static class VisiblePointMarkerPolicy
{
    public const int DefaultVisiblePointThreshold = 200;

    public static bool ShouldShowMarkers(
        IEnumerable<FixedXyPoint> points,
        XRange visibleXRange,
        int threshold = DefaultVisiblePointThreshold)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(threshold);

        var count = 0;
        foreach (var point in points)
        {
            if (!double.IsFinite(point.X) || !double.IsFinite(point.Y)
                || point.X < visibleXRange.Minimum || point.X > visibleXRange.Maximum)
            {
                continue;
            }

            count++;
            if (count >= threshold)
            {
                return false;
            }
        }

        return count > 0;
    }
}
