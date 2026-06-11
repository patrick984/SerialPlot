using System;
using System.Collections.Generic;
namespace SerialPlot.Services;

public sealed class HoverPointIndex
{
    private readonly List<HoverPoint> _points = [];

    public int CandidateCountLastSearch { get; private set; }
    public int PointCount => _points.Count;

    public void Rebuild(IEnumerable<FixedXyPoint> points, XRange visibleXRange)
    {
        _points.Clear();
        foreach (var point in points)
        {
            if (double.IsFinite(point.X) && double.IsFinite(point.Y)
                && point.X >= visibleXRange.Minimum && point.X <= visibleXRange.Maximum)
            {
                _points.Add(new HoverPoint(point.Index, point.X, point.Y));
            }
        }

        _points.Sort(static (left, right) => left.X.CompareTo(right.X));
    }

    public HoverPointHit? FindNearest(
        double mousePixelX,
        double mousePixelY,
        XRange candidateXRange,
        Func<double, double, (double PixelX, double PixelY)> toPixel,
        double maxDistance)
    {
        CandidateCountLastSearch = 0;
        if (_points.Count == 0 || maxDistance <= 0 || !double.IsFinite(candidateXRange.Minimum) || !double.IsFinite(candidateXRange.Maximum))
        {
            return null;
        }

        var minX = Math.Min(candidateXRange.Minimum, candidateXRange.Maximum);
        var maxX = Math.Max(candidateXRange.Minimum, candidateXRange.Maximum);
        var index = LowerBound(minX);
        var maxDistanceSquared = maxDistance * maxDistance;
        HoverPointHit? nearest = null;

        for (var i = index; i < _points.Count && _points[i].X <= maxX; i++)
        {
            CandidateCountLastSearch++;
            var point = _points[i];
            var pixel = toPixel(point.X, point.Y);
            var dx = pixel.PixelX - mousePixelX;
            var dy = pixel.PixelY - mousePixelY;
            var distanceSquared = (dx * dx) + (dy * dy);
            if (distanceSquared <= maxDistanceSquared && (nearest is null || distanceSquared < nearest.Value.DistanceSquared))
            {
                nearest = new HoverPointHit(point.Index, point.X, point.Y, distanceSquared);
            }
        }

        return nearest;
    }

    private int LowerBound(double x)
    {
        var low = 0;
        var high = _points.Count;
        while (low < high)
        {
            var mid = low + ((high - low) / 2);
            if (_points[mid].X < x)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }

        return low;
    }
}

public readonly record struct HoverPoint(int Index, double X, double Y);

public readonly record struct HoverPointHit(int Index, double X, double Y, double DistanceSquared);
