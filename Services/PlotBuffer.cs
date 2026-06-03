using System;
using System.Collections.Generic;
using SerialPlot.Models;

namespace SerialPlot.Services;

public sealed class PlotBuffer
{
    private readonly int _capacity;
    private readonly double[][] _rows;
    private int _start;
    private int _count;

    public PlotBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _capacity = capacity;
        _rows = new double[capacity][];
    }

    public int Count => _count;

    public void Add(IReadOnlyList<ParsedCell> cells)
    {
        var row = new double[cells.Count];
        for (var i = 0; i < cells.Count; i++)
        {
            row[i] = cells[i].IsValid ? cells[i].NumericValue : double.NaN;
        }

        if (_count < _capacity)
        {
            _rows[(_start + _count) % _capacity] = row;
            _count++;
            return;
        }

        _rows[_start] = row;
        _start = (_start + 1) % _capacity;
    }

    public void Clear()
    {
        Array.Clear(_rows);
        _start = 0;
        _count = 0;
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
}

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
