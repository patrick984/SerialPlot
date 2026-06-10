using System.Linq;
using SerialPlot.Models;
using SerialPlot.Services;
using Xunit;

namespace SerialPlot.Tests;

public sealed class PlotBufferTests
{
    [Fact]
    public void CircularBufferCapsRowsAndPreservesOrder()
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
    public void CircularBufferPreservesOrderAcrossMultipleWraps()
    {
        var buffer = new PlotBuffer(3);
        for (var i = 1; i <= 7; i++)
        {
            buffer.Add([new ParsedCell(i, null, true), new ParsedCell(i * 10, null, true)]);
        }

        var (xs, ys) = buffer.GetSeries(0, 1);

        Assert.Equal([5, 6, 7], xs);
        Assert.Equal([50, 60, 70], ys);
    }

    [Fact]
    public void VersionIncrementsAndRowsEnumerateChronologicallyAfterWrap()
    {
        var buffer = new PlotBuffer(3);
        Assert.Equal(0, buffer.Version);

        for (var i = 1; i <= 5; i++)
        {
            buffer.Add([new ParsedCell(i, null, true)]);
        }

        Assert.Equal(5, buffer.Version);
        Assert.Equal(3, buffer.OldestVersion);
        Assert.Equal(
            [(3L, 3d), (4L, 4d), (5L, 5d)],
            buffer.EnumerateRows().Select(x => (x.Version, x.Values[0])).ToArray());
    }

    [Fact]
    public void ClearResetsCircularBuffer()
    {
        var buffer = new PlotBuffer(2);
        buffer.Add([new ParsedCell(1, null, true)]);
        buffer.Add([new ParsedCell(2, null, true)]);
        buffer.Clear();
        buffer.Add([new ParsedCell(3, null, true)]);

        var (xs, ys) = buffer.GetSeries(0, 0);

        Assert.Single(xs);
        Assert.Equal(3, xs[0]);
        Assert.Equal(3, ys[0]);
    }

    [Fact]
    public void ClearIncrementsVersionAndResetsOldestVersion()
    {
        var buffer = new PlotBuffer(2);
        buffer.Add([new ParsedCell(1, null, true)]);
        buffer.Clear();

        Assert.Equal(2, buffer.Version);
        Assert.Equal(buffer.Version, buffer.OldestVersion);
        Assert.Empty(buffer.EnumerateRows());
    }

    [Fact]
    public void CopyValidPairsDropsInvalidMissingAndNonFiniteSamples()
    {
        var buffer = new PlotBuffer(8);
        buffer.Add([new ParsedCell(1, null, true), new ParsedCell(10, null, true)]);
        buffer.Add([new ParsedCell(2, null, true), ParsedCell.Gap]);
        buffer.Add([ParsedCell.Gap, new ParsedCell(30, null, true)]);
        buffer.Add([new ParsedCell(double.NaN, null, true), new ParsedCell(40, null, true)]);
        buffer.Add([new ParsedCell(5, null, true)]);
        buffer.Add([new ParsedCell(6, null, true), new ParsedCell(double.PositiveInfinity, null, true)]);
        buffer.Add([new ParsedCell(7, null, true), new ParsedCell(70, null, true)]);

        var xs = new double[8];
        var ys = new double[8];
        var length = buffer.CopyValidPairs(0, 1, xs, ys);

        Assert.Equal(2, length);
        Assert.Equal([1, 7], xs.Take(length).ToArray());
        Assert.Equal([10, 70], ys.Take(length).ToArray());
        Assert.All(xs.Take(length), x => Assert.True(double.IsFinite(x)));
        Assert.All(ys.Take(length), y => Assert.True(double.IsFinite(y)));
    }

    [Fact]
    public void CopyValidPairsSinceOnlyCopiesNewerRows()
    {
        var buffer = new PlotBuffer(4);
        buffer.Add([new ParsedCell(1, null, true), new ParsedCell(10, null, true)]);
        buffer.Add([new ParsedCell(2, null, true), new ParsedCell(20, null, true)]);
        var version = buffer.Version;
        buffer.Add([new ParsedCell(3, null, true), new ParsedCell(30, null, true)]);

        var xs = new double[4];
        var ys = new double[4];
        var length = buffer.CopyValidPairsSince(version, 0, 1, xs, ys);

        Assert.Equal(1, length);
        Assert.Equal(3, xs[0]);
        Assert.Equal(30, ys[0]);
    }

    [Fact]
    public void CopyValidPairsSinceAfterWrapSkipsOnlyOldestRowWhenAfterVersionIsOldestVersion()
    {
        var buffer = new PlotBuffer(3);
        for (var i = 1; i <= 5; i++)
        {
            buffer.Add([new ParsedCell(i, null, true), new ParsedCell(i * 10, null, true)]);
        }

        var xs = new double[3];
        var ys = new double[3];
        var length = buffer.CopyValidPairsSince(buffer.OldestVersion, 0, 1, xs, ys);

        Assert.Equal(2, length);
        Assert.Equal([4, 5], xs.Take(length).ToArray());
        Assert.Equal([40, 50], ys.Take(length).ToArray());
    }

    [Fact]
    public void CopyValidPairsSinceAfterWrapCopiesOnlyTailRows()
    {
        var buffer = new PlotBuffer(3);
        for (var i = 1; i <= 5; i++)
        {
            buffer.Add([new ParsedCell(i, null, true), new ParsedCell(i * 10, null, true)]);
        }

        var xs = new double[3];
        var ys = new double[3];
        var length = buffer.CopyValidPairsSince(buffer.Version - 1, 0, 1, xs, ys);

        Assert.Equal(1, length);
        Assert.Equal(5, xs[0]);
        Assert.Equal(50, ys[0]);
    }

    [Fact]
    public void CopyValidPairsSinceAfterWrapCopiesAllRetainedRowsWhenAfterVersionIsStale()
    {
        var buffer = new PlotBuffer(3);
        for (var i = 1; i <= 5; i++)
        {
            buffer.Add([new ParsedCell(i, null, true), new ParsedCell(i * 10, null, true)]);
        }

        var xs = new double[3];
        var ys = new double[3];
        var length = buffer.CopyValidPairsSince(buffer.OldestVersion - 1, 0, 1, xs, ys);

        Assert.Equal(3, length);
        Assert.Equal([3, 4, 5], xs.Take(length).ToArray());
        Assert.Equal([30, 40, 50], ys.Take(length).ToArray());
    }

    [Fact]
    public void FixedXyRingBufferReportsEmptyPartialFullWrappedAndClearedSegments()
    {
        var ring = new FixedXyRingBuffer(3);
        Assert.Equal(RingSegments.Empty, ring.GetSegments());

        ring.Append(1, 10);
        ring.Append(2, 20);
        Assert.Equal(new RingSegments(new RingIndexRange(0, 1), null), ring.GetSegments());

        ring.Append(3, 30);
        Assert.Equal(new RingSegments(new RingIndexRange(0, 2), null), ring.GetSegments());

        ring.Append(4, 40);
        Assert.Equal(new RingSegments(new RingIndexRange(1, 2), new RingIndexRange(0, 0)), ring.GetSegments());

        ring.Clear();
        Assert.Equal(RingSegments.Empty, ring.GetSegments());
    }

    [Fact]
    public void FixedXyRingBufferDropsInvalidAppendsAndNeverWritesNaN()
    {
        var ring = new FixedXyRingBuffer(2);
        ring.Append(1, 10);
        ring.Append(double.NaN, 20);
        ring.Append(2, double.PositiveInfinity);
        ring.Append(3, 30);

        Assert.Equal(2, ring.Count);
        Assert.Equal([1, 3], ring.Xs);
        Assert.Equal([10, 30], ring.Ys);
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
