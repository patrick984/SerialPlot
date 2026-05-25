using System;
using System.Collections.Generic;
using System.Linq;
using SerialPlot.Models;

namespace SerialPlot.Services;

public sealed class PlotBuffer
{
    private readonly int _capacity;
    private readonly List<double[]> _rows = [];

    public PlotBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _capacity = capacity;
    }

    public int Count => _rows.Count;

    public void Add(IReadOnlyList<ParsedCell> cells)
    {
        if (_rows.Count == _capacity)
        {
            _rows.RemoveAt(0);
        }

        _rows.Add(cells.Select(c => c.IsValid ? c.NumericValue : double.NaN).ToArray());
    }

    public void Clear() => _rows.Clear();

    public (double[] Xs, double[] Ys) GetSeries(int xIndex, int yIndex)
    {
        var xs = new double[_rows.Count];
        var ys = new double[_rows.Count];
        for (var i = 0; i < _rows.Count; i++)
        {
            var row = _rows[i];
            xs[i] = xIndex >= 0 && xIndex < row.Length ? row[xIndex] : double.NaN;
            ys[i] = yIndex >= 0 && yIndex < row.Length ? row[yIndex] : double.NaN;
        }

        return (xs, ys);
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
