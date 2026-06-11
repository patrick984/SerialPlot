using System;

namespace SerialPlot.Services;

public readonly record struct XRange(double Minimum, double Maximum)
{
    public double Width => Maximum - Minimum;
}

public sealed class SteppedXAxisViewport
{
    public const double ExpansionSeconds = 10;
    public const double ExpansionThreshold = 0.9;

    private XRange? _range;

    public XRange? Current => _range;

    public void Reset() => _range = null;

    public XRange? Update(double oldestX, double newestX, double sampleRatePerSecond, double recentXSpacing)
    {
        if (!double.IsFinite(oldestX) || !double.IsFinite(newestX))
        {
            return _range;
        }

        if (newestX < oldestX)
        {
            (oldestX, newestX) = (newestX, oldestX);
        }

        if (_range is not { } current || newestX < current.Minimum || oldestX > current.Maximum)
        {
            _range = CreateRange(oldestX, newestX, sampleRatePerSecond, recentXSpacing);
            return _range;
        }

        var width = current.Width;
        if (width <= 0 || newestX >= current.Minimum + (width * ExpansionThreshold))
        {
            var futureSpan = EstimateFutureSpan(sampleRatePerSecond, recentXSpacing);
            _range = new XRange(Math.Min(current.Minimum, oldestX), newestX + futureSpan);
        }

        return _range;
    }

    private static XRange CreateRange(double oldestX, double newestX, double sampleRatePerSecond, double recentXSpacing)
    {
        var futureSpan = EstimateFutureSpan(sampleRatePerSecond, recentXSpacing);
        var minimum = oldestX;
        var maximum = newestX + futureSpan;
        if (maximum <= minimum)
        {
            maximum = minimum + futureSpan;
        }

        return new XRange(minimum, maximum);
    }

    private static double EstimateFutureSpan(double sampleRatePerSecond, double recentXSpacing)
    {
        var span = sampleRatePerSecond > 0 && double.IsFinite(sampleRatePerSecond)
            && recentXSpacing > 0 && double.IsFinite(recentXSpacing)
            ? sampleRatePerSecond * ExpansionSeconds * recentXSpacing
            : 1;

        return span > 0 && double.IsFinite(span) ? span : 1;
    }
}
