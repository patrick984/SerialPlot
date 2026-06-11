using System;
using System.Collections.Generic;

namespace SerialPlot.Services;

public sealed class FixedXyRingBuffer
{
    private int _start;
    private int _nextIndex;

    public FixedXyRingBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        Xs = new double[capacity];
        Ys = new double[capacity];
    }

    public double[] Xs { get; }
    public double[] Ys { get; }
    public int Capacity => Xs.Length;
    public int Count { get; private set; }

    public void Append(double x, double y)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y))
        {
            return;
        }

        Xs[_nextIndex] = x;
        Ys[_nextIndex] = y;

        if (Count < Capacity)
        {
            Count++;
        }
        else
        {
            _start = (_start + 1) % Capacity;
        }

        _nextIndex = (_nextIndex + 1) % Capacity;
    }

    public void Clear()
    {
        _start = 0;
        _nextIndex = 0;
        Count = 0;
    }

    public void Rebuild(ReadOnlySpan<double> xs, ReadOnlySpan<double> ys, int length)
    {
        Clear();
        var count = Math.Min(length, Math.Min(xs.Length, ys.Length));
        for (var i = 0; i < count; i++)
        {
            Append(xs[i], ys[i]);
        }
    }

    public bool TryGetOldestAndNewestX(out double oldestX, out double newestX)
    {
        oldestX = double.NaN;
        newestX = double.NaN;
        if (Count == 0)
        {
            return false;
        }

        var newestIndex = (_nextIndex - 1 + Capacity) % Capacity;
        oldestX = Xs[_start];
        newestX = Xs[newestIndex];
        return double.IsFinite(oldestX) && double.IsFinite(newestX);
    }

    public bool TryGetRecentXSpacing(out double spacing)
    {
        spacing = double.NaN;
        double? newer = null;
        for (var ordinal = Count - 1; ordinal >= 0; ordinal--)
        {
            var index = (_start + ordinal) % Capacity;
            var x = Xs[index];
            if (!double.IsFinite(x))
            {
                continue;
            }

            if (newer is { } value)
            {
                var delta = value - x;
                if (delta > 0 && double.IsFinite(delta))
                {
                    spacing = delta;
                    return true;
                }
            }

            newer = x;
        }

        return false;
    }

    public FixedXyPoint? FindNearest(double mousePixelX, double mousePixelY, Func<double, double, (double PixelX, double PixelY)> toPixel, double maxDistance)
    {
        FixedXyPoint? nearest = null;
        var maxDistanceSquared = maxDistance * maxDistance;

        foreach (var point in EnumeratePoints())
        {
            var pixel = toPixel(point.X, point.Y);
            var dx = pixel.PixelX - mousePixelX;
            var dy = pixel.PixelY - mousePixelY;
            var distanceSquared = (dx * dx) + (dy * dy);
            if (distanceSquared <= maxDistanceSquared && (nearest is null || distanceSquared < nearest.Value.DistanceSquared))
            {
                nearest = point with { DistanceSquared = distanceSquared };
            }
        }

        return nearest;
    }

    public IEnumerable<FixedXyPoint> EnumeratePoints()
    {
        for (var i = 0; i < Count; i++)
        {
            var index = (_start + i) % Capacity;
            var x = Xs[index];
            var y = Ys[index];
            if (double.IsFinite(x) && double.IsFinite(y))
            {
                yield return new FixedXyPoint(index, x, y, double.PositiveInfinity);
            }
        }
    }

    public RingSegments GetSegments()
    {
        if (Count == 0)
        {
            return RingSegments.Empty;
        }

        if (Count < Capacity || _start == 0)
        {
            return new RingSegments(new RingIndexRange(0, Count - 1), null);
        }

        return new RingSegments(
            new RingIndexRange(_start, Capacity - 1),
            new RingIndexRange(0, _nextIndex - 1));
    }
}

public readonly record struct RingSegments(RingIndexRange? Older, RingIndexRange? Newer)
{
    public static RingSegments Empty { get; } = new(null, null);
}

public readonly record struct RingIndexRange(int Minimum, int Maximum);

public readonly record struct FixedXyPoint(int Index, double X, double Y, double DistanceSquared);
