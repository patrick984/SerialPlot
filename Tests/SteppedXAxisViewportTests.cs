using SerialPlot.Services;
using Xunit;

namespace SerialPlot.Tests;

public sealed class SteppedXAxisViewportTests
{
    [Fact]
    public void FirstValidDataInitializesViewportWithFutureSpace()
    {
        var viewport = new SteppedXAxisViewport();

        var range = viewport.Update(0, 5, sampleRatePerSecond: 10, recentXSpacing: 0.1);

        Assert.Equal(new XRange(0, 15), range);
    }

    [Fact]
    public void DoesNotExpandBeforeNewestReachesThreshold()
    {
        var viewport = new SteppedXAxisViewport();
        var initial = viewport.Update(0, 5, sampleRatePerSecond: 10, recentXSpacing: 0.1);

        var range = viewport.Update(0, 12, sampleRatePerSecond: 10, recentXSpacing: 0.1);

        Assert.Equal(initial, range);
    }

    [Fact]
    public void ExpandsWhenNewestReachesThreshold()
    {
        var viewport = new SteppedXAxisViewport();
        viewport.Update(0, 5, sampleRatePerSecond: 10, recentXSpacing: 0.1);

        var range = viewport.Update(0, 14, sampleRatePerSecond: 10, recentXSpacing: 0.1);

        Assert.Equal(new XRange(0, 24), range);
    }

    [Fact]
    public void UsesSampleRateAndRecentXSpacingForFutureSpan()
    {
        var viewport = new SteppedXAxisViewport();

        var range = viewport.Update(100, 110, sampleRatePerSecond: 5, recentXSpacing: 2);

        Assert.Equal(new XRange(100, 210), range);
    }

    [Fact]
    public void InvalidEstimateFallsBackToSmallPositiveSpan()
    {
        var viewport = new SteppedXAxisViewport();

        var range = viewport.Update(5, 5, sampleRatePerSecond: 0, recentXSpacing: 0);

        Assert.Equal(new XRange(5, 6), range);
    }
}
