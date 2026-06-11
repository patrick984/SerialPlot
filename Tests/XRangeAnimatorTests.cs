using System;
using SerialPlot.Services;
using Xunit;

namespace SerialPlot.Tests;

public sealed class XRangeAnimatorTests
{
    [Fact]
    public void TickInterpolatesWithEaseOutAndCompletesAtTarget()
    {
        var animator = new XRangeAnimator();
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        animator.Retarget(new XRange(0, 10), new XRange(10, 20), start, TimeSpan.FromMilliseconds(300));
        var middle = animator.Tick(start.AddMilliseconds(150));
        var end = animator.Tick(start.AddMilliseconds(300));

        Assert.True(middle.Minimum > 5);
        Assert.Equal(new XRange(10, 20), end);
        Assert.False(animator.IsActive);
    }

    [Fact]
    public void ResetClearsActiveAnimation()
    {
        var animator = new XRangeAnimator();
        animator.Retarget(new XRange(0, 10), new XRange(10, 20), DateTime.UtcNow);

        animator.Reset();

        Assert.False(animator.IsActive);
        Assert.Null(animator.Target);
    }
}
