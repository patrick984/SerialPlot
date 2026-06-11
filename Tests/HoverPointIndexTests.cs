using System.Collections.Generic;
using SerialPlot.Services;
using Xunit;

namespace SerialPlot.Tests;

public sealed class HoverPointIndexTests
{
    [Fact]
    public void SearchUsesIndexedCandidatesInsteadOfAllPoints()
    {
        var index = new HoverPointIndex();
        index.Rebuild(CreatePoints(1000), new XRange(0, 999));

        var hit = index.FindNearest(
            mousePixelX: 150.2,
            mousePixelY: 5,
            candidateXRange: new XRange(148, 152),
            toPixel: static (x, y) => (x, y),
            maxDistance: 30);

        Assert.NotNull(hit);
        Assert.Equal(150, hit.Value.X);
        Assert.True(index.CandidateCountLastSearch < index.PointCount);
    }

    [Fact]
    public void RebuildIgnoresInvalidAndOutsideVisibleXRangePoints()
    {
        var index = new HoverPointIndex();
        var points = new[]
        {
            new FixedXyPoint(0, 0, 0, 0),
            new FixedXyPoint(1, 10, 10, 0),
            new FixedXyPoint(2, double.NaN, 10, 0),
            new FixedXyPoint(3, 20, double.NaN, 0),
        };

        index.Rebuild(points, new XRange(5, 15));

        Assert.Equal(1, index.PointCount);
    }

    [Fact]
    public void SearchReturnsNullWhenNoCandidateWithinHitRadius()
    {
        var index = new HoverPointIndex();
        index.Rebuild(CreatePoints(10), new XRange(0, 9));

        var hit = index.FindNearest(
            mousePixelX: 100,
            mousePixelY: 100,
            candidateXRange: new XRange(0, 9),
            toPixel: static (x, y) => (x, y),
            maxDistance: 5);

        Assert.Null(hit);
    }

    private static IEnumerable<FixedXyPoint> CreatePoints(int count)
    {
        for (var i = 0; i < count; i++)
        {
            yield return new FixedXyPoint(i, i, 5, 0);
        }
    }
}
