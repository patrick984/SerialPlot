using System.Collections.Generic;
using SerialPlot.Services;
using Xunit;

namespace SerialPlot.Tests;

public sealed class VisiblePointMarkerPolicyTests
{
    [Fact]
    public void ShowsMarkersWhenVisiblePointCountIsBelowThreshold()
    {
        var points = CreatePoints(199);

        var shouldShow = VisiblePointMarkerPolicy.ShouldShowMarkers(points, new XRange(0, 198));

        Assert.True(shouldShow);
    }

    [Fact]
    public void HidesMarkersWhenVisiblePointCountReachesThreshold()
    {
        var points = CreatePoints(200);

        var shouldShow = VisiblePointMarkerPolicy.ShouldShowMarkers(points, new XRange(0, 199));

        Assert.False(shouldShow);
    }

    [Fact]
    public void IgnoresInvalidAndOutsideVisibleRangePoints()
    {
        var points = new[]
        {
            new FixedXyPoint(0, -1, 1, 0),
            new FixedXyPoint(1, 1, 1, 0),
            new FixedXyPoint(2, 2, double.NaN, 0),
            new FixedXyPoint(3, 10, 1, 0),
        };

        var shouldShow = VisiblePointMarkerPolicy.ShouldShowMarkers(points, new XRange(0, 5), threshold: 2);

        Assert.True(shouldShow);
    }

    [Fact]
    public void CountingShortCircuitsAtThreshold()
    {
        var points = new CountingEnumerable(CreatePoints(1_000));

        var shouldShow = VisiblePointMarkerPolicy.ShouldShowMarkers(points, new XRange(0, 999), threshold: 200);

        Assert.False(shouldShow);
        Assert.Equal(200, points.MoveNextCount);
    }

    private static IEnumerable<FixedXyPoint> CreatePoints(int count)
    {
        for (var i = 0; i < count; i++)
        {
            yield return new FixedXyPoint(i, i, i, 0);
        }
    }

    private sealed class CountingEnumerable(IEnumerable<FixedXyPoint> inner) : IEnumerable<FixedXyPoint>
    {
        public int MoveNextCount { get; private set; }

        public IEnumerator<FixedXyPoint> GetEnumerator()
        {
            foreach (var point in inner)
            {
                MoveNextCount++;
                yield return point;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
