using System;

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
