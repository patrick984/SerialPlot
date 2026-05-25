using System;
using SerialPlot.Models;
using SerialPlot.Services;
using Xunit;

namespace SerialPlot.Tests;

public sealed class CsvStreamParserTests
{
    [Fact]
    public void HeaderRejectsBlankAndDuplicateNames()
    {
        var parser = new CsvStreamParser(TimestampUnit.Auto);

        Assert.Throws<CsvSchemaException>(() => parser.ParseHeader("time,,value"));
        Assert.Throws<CsvSchemaException>(() => parser.ParseHeader("time,value,value"));
    }

    [Fact]
    public void RowRejectsMismatchedColumnCount()
    {
        var parser = new CsvStreamParser(TimestampUnit.Auto);
        var schema = parser.ParseHeader("time,value");

        Assert.Throws<CsvSchemaException>(() => parser.ParseRow(schema, "1,2,3"));
    }

    [Fact]
    public void ParsesNumbersDatesUnixAndGaps()
    {
        var parser = new CsvStreamParser(TimestampUnit.Milliseconds);
        var schema = parser.ParseHeader("a,b,c,d,e");
        var row = parser.ParseRow(schema, "12,3.5,2024-01-02T03:04:05Z,1700000000000,");

        Assert.True(row.Cells[0].IsValid);
        Assert.Equal(12, row.Cells[0].NumericValue);
        Assert.True(row.Cells[1].IsValid);
        Assert.Equal(3.5, row.Cells[1].NumericValue);
        Assert.True(row.Cells[2].DateTimeValue.HasValue);
        Assert.True(row.Cells[3].DateTimeValue.HasValue);
        Assert.False(row.Cells[4].IsValid);
    }

    [Fact]
    public void ColumnEligibilityFollowsObservedValues()
    {
        var numeric = new ColumnState("numeric");
        numeric.Observe(new ParsedCell(1, null, true));
        Assert.True(numeric.CanBeX);
        Assert.True(numeric.CanBeY);

        var date = new ColumnState("date");
        date.Observe(new ParsedCell(1, DateTimeOffset.UnixEpoch, true));
        Assert.True(date.CanBeX);
        Assert.False(date.CanBeY);
    }
}
