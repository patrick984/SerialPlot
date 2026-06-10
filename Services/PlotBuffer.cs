using System;
using System.Collections.Generic;
using SerialPlot.Models;

namespace SerialPlot.Services;

public sealed class PlotBuffer
{
    private readonly int _capacity;
    private readonly double[][] _rows;
    private readonly long[] _rowVersions;
    private int _start;
    private int _count;
    private long _version;

    public PlotBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _capacity = capacity;
        _rows = new double[capacity][];
        _rowVersions = new long[capacity];
    }

    public int Count => _count;
    public int Capacity => _capacity;
    public long Version => _version;
    public long OldestVersion => _count == 0 ? _version : _rowVersions[_start];

    public void Add(IReadOnlyList<ParsedCell> cells)
    {
        var row = new double[cells.Count];
        for (var i = 0; i < cells.Count; i++)
        {
            row[i] = cells[i].IsValid ? cells[i].NumericValue : double.NaN;
        }

        _version++;
        if (_count < _capacity)
        {
            var index = (_start + _count) % _capacity;
            _rows[index] = row;
            _rowVersions[index] = _version;
            _count++;
            return;
        }

        _rows[_start] = row;
        _rowVersions[_start] = _version;
        _start = (_start + 1) % _capacity;
    }

    public void Clear()
    {
        Array.Clear(_rows);
        Array.Clear(_rowVersions);
        _start = 0;
        _count = 0;
        _version++;
    }

    public (double[] Xs, double[] Ys) GetSeries(int xIndex, int yIndex)
    {
        var xs = new double[_count];
        var ys = new double[_count];
        CopySeries(xIndex, yIndex, xs, ys);
        return (xs, ys);
    }

    public int CopySeries(int xIndex, int yIndex, double[] xs, double[] ys)
    {
        var length = Math.Min(_count, Math.Min(xs.Length, ys.Length));
        for (var i = 0; i < length; i++)
        {
            var row = _rows[(_start + i) % _capacity];
            xs[i] = xIndex >= 0 && xIndex < row.Length ? row[xIndex] : double.NaN;
            ys[i] = yIndex >= 0 && yIndex < row.Length ? row[yIndex] : double.NaN;
        }

        return length;
    }

    public int CopyValidPairs(int xIndex, int yIndex, double[] xs, double[] ys)
    {
        var written = 0;
        foreach (var row in EnumerateRows())
        {
            if (TryGetValidPair(row, xIndex, yIndex, out var x, out var y))
            {
                if (written >= xs.Length || written >= ys.Length)
                {
                    break;
                }

                xs[written] = x;
                ys[written] = y;
                written++;
            }
        }

        return written;
    }

    public int CopyValidPairsSince(long afterVersion, int xIndex, int yIndex, double[] xs, double[] ys)
    {
        var written = 0;
        foreach (var row in EnumerateRowsSince(afterVersion))
        {
            if (TryGetValidPair(row, xIndex, yIndex, out var x, out var y))
            {
                if (written >= xs.Length || written >= ys.Length)
                {
                    break;
                }

                xs[written] = x;
                ys[written] = y;
                written++;
            }
        }

        return written;
    }

    public IEnumerable<PlotBufferRow> EnumerateRows()
    {
        for (var i = 0; i < _count; i++)
        {
            var index = (_start + i) % _capacity;
            yield return new PlotBufferRow(_rowVersions[index], _rows[index]);
        }
    }

    public IEnumerable<PlotBufferRow> EnumerateRowsSince(long afterVersion)
    {
        foreach (var row in EnumerateRows())
        {
            if (row.Version > afterVersion)
            {
                yield return row;
            }
        }
    }

    private static bool TryGetValidPair(PlotBufferRow row, int xIndex, int yIndex, out double x, out double y)
    {
        x = xIndex >= 0 && xIndex < row.Values.Count ? row.Values[xIndex] : double.NaN;
        y = yIndex >= 0 && yIndex < row.Values.Count ? row.Values[yIndex] : double.NaN;
        return double.IsFinite(x) && double.IsFinite(y);
    }
}

public readonly record struct PlotBufferRow(long Version, IReadOnlyList<double> Values);

public sealed class RawCsvBuffer(int capacity)
{
    private readonly List<string> _lines = [];

    public IReadOnlyList<string> Lines => _lines;

    public void Add(string line)
    {
        if (_lines.Count == capacity)
        {
            _lines.RemoveAt(0);
        }

        _lines.Add(line);
    }

    public void Clear() => _lines.Clear();

    public string ToCsvText() => string.Join(Environment.NewLine, _lines) + (_lines.Count > 0 ? Environment.NewLine : string.Empty);
}
