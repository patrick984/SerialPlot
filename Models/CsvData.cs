using System;
using System.Collections.Generic;

namespace SerialPlot.Models;

public enum ColumnKind
{
    Unknown,
    Numeric,
    DateTime,
}

public enum TraceAxisSide
{
    Left,
    Right,
}

public readonly record struct ParsedCell(double NumericValue, DateTimeOffset? DateTimeValue, bool IsValid)
{
    public static ParsedCell Gap { get; } = new(double.NaN, null, false);
}

public sealed record ParsedRow(IReadOnlyList<string> RawFields, IReadOnlyList<ParsedCell> Cells, string RawLine);

public sealed class ColumnState(string name)
{
    public string Name { get; } = name;
    public bool HasNumeric { get; private set; }
    public bool HasDateTime { get; private set; }
    public ColumnKind Kind => HasNumeric ? ColumnKind.Numeric : HasDateTime ? ColumnKind.DateTime : ColumnKind.Unknown;
    public bool CanBeX => HasNumeric || HasDateTime;
    public bool CanBeY => HasNumeric;

    public void Observe(ParsedCell cell)
    {
        if (!cell.IsValid)
        {
            return;
        }

        if (cell.DateTimeValue.HasValue)
        {
            HasDateTime = true;
        }
        else
        {
            HasNumeric = true;
        }
    }
}

public sealed record CsvSchema(IReadOnlyList<string> Headers);
