using SerialPlot.Models;
using SerialPlot.Services;
using Xunit;

namespace SerialPlot.Tests;

public sealed class SteppedXAxisViewportTests
{
    [Fact]
    public void FirstValidDataInitializesViewportWithFutureSpace()
    {
        var viewport = new SteppedXAxisViewport();

        var range = viewport.Update(0, 5, XAutoscaleMode.SteppedExpansion, visibleRange: null, sampleRatePerSecond: 10, recentXSpacing: 0.1);

        Assert.Equal(new XRange(0, 35), range);
    }

    [Fact]
    public void UsesCustomFutureSpaceSeconds()
    {
        var viewport = new SteppedXAxisViewport();

        var range = viewport.Update(0, 5, XAutoscaleMode.SteppedExpansion, visibleRange: null, sampleRatePerSecond: 10, recentXSpacing: 0.1, futureSpaceSeconds: 5);

        Assert.Equal(new XRange(0, 10), range);
    }

    [Fact]
    public void DoesNotExpandBeforeNewestReachesThreshold()
    {
        var viewport = new SteppedXAxisViewport();
        var initial = viewport.Update(0, 5, XAutoscaleMode.SteppedExpansion, visibleRange: null, sampleRatePerSecond: 10, recentXSpacing: 0.1);

        var range = viewport.Update(0, 31.4, XAutoscaleMode.SteppedExpansion, initial, sampleRatePerSecond: 10, recentXSpacing: 0.1);

        Assert.Equal(initial, range);
    }

    [Fact]
    public void ExpandsWhenNewestReachesThreshold()
    {
        var viewport = new SteppedXAxisViewport();
        var initial = viewport.Update(0, 5, XAutoscaleMode.SteppedExpansion, visibleRange: null, sampleRatePerSecond: 10, recentXSpacing: 0.1);

        var range = viewport.Update(0, 31.5, XAutoscaleMode.SteppedExpansion, initial, sampleRatePerSecond: 10, recentXSpacing: 0.1);

        Assert.Equal(new XRange(0, 61.5), range);
    }

    [Fact]
    public void ExpansionModeExpandsEvenWhenZoomedIntoRetainedRange()
    {
        var viewport = new SteppedXAxisViewport();

        var range = viewport.Update(
            oldestX: 0,
            newestX: 112,
            mode: XAutoscaleMode.SteppedExpansion,
            visibleRange: new XRange(20, 120),
            sampleRatePerSecond: 1,
            recentXSpacing: 1);

        Assert.Equal(new XRange(0, 142), range);
    }

    [Fact]
    public void PanModePreservesVisibleWidthWhenNewestReachesThreshold()
    {
        var viewport = new SteppedXAxisViewport();

        var range = viewport.Update(
            oldestX: 0,
            newestX: 112,
            mode: XAutoscaleMode.SteppedPan,
            visibleRange: new XRange(20, 120),
            sampleRatePerSecond: 1,
            recentXSpacing: 1);

        Assert.Equal(100, range?.Width);
        Assert.Equal(new XRange(42, 142), range);
    }

    [Fact]
    public void PanModePreservesVisibleWidthWhenRetainedRangeMovesOutsideView()
    {
        var viewport = new SteppedXAxisViewport();

        var range = viewport.Update(
            oldestX: 250,
            newestX: 300,
            mode: XAutoscaleMode.SteppedPan,
            visibleRange: new XRange(0, 100),
            sampleRatePerSecond: 1,
            recentXSpacing: 1);

        Assert.Equal(new XRange(230, 330), range);
    }

    [Fact]
    public void ResetClearsCurrentTarget()
    {
        var viewport = new SteppedXAxisViewport();
        viewport.Update(0, 5, XAutoscaleMode.SteppedExpansion, visibleRange: null, sampleRatePerSecond: 10, recentXSpacing: 0.1);

        viewport.Reset();

        Assert.Null(viewport.Current);
    }

    [Fact]
    public void InvalidEstimateFallsBackToSmallPositiveSpan()
    {
        var viewport = new SteppedXAxisViewport();

        var range = viewport.Update(5, 5, XAutoscaleMode.SteppedExpansion, visibleRange: null, sampleRatePerSecond: 0, recentXSpacing: 0);

        Assert.Equal(new XRange(5, 6), range);
    }
}
