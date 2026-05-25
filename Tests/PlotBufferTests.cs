using SerialPlot.Models;
using SerialPlot.Services;
using Xunit;

namespace SerialPlot.Tests;

public sealed class PlotBufferTests
{
    [Fact]
    public void CircularBufferCapsRowsAndPreservesGaps()
    {
        var buffer = new PlotBuffer(2);
        buffer.Add([new ParsedCell(1, null, true), new ParsedCell(10, null, true)]);
        buffer.Add([new ParsedCell(2, null, true), ParsedCell.Gap]);
        buffer.Add([new ParsedCell(3, null, true), new ParsedCell(30, null, true)]);

        var (xs, ys) = buffer.GetSeries(0, 1);

        Assert.Equal([2, 3], xs);
        Assert.True(double.IsNaN(ys[0]));
        Assert.Equal(30, ys[1]);
    }

    [Fact]
    public void RawCsvBufferKeepsExactLines()
    {
        var buffer = new RawCsvBuffer(2);
        buffer.Add("a,b");
        buffer.Add("\"1,2\",3");
        buffer.Add("4,5");

        Assert.Equal("\"1,2\",3", buffer.Lines[0]);
        Assert.Equal("4,5", buffer.Lines[1]);
    }
}
