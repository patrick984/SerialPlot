using SerialPlot.Views;
using Xunit;

namespace SerialPlot.Tests;

public sealed class AxisToggleMetricsTests
{
    [Theory]
    [InlineData(1.0)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    [InlineData(2.0)]
    [InlineData(2.5)]
    public void CheckGlyphStaysCenteredInFluentCheckboxFootprintAcrossDpiScales(double dpiScale)
    {
        var metrics = AxisToggleMetrics.Scale(dpiScale);
        var tolerance = 0.01 * dpiScale;

        Assert.Equal(20 * dpiScale, metrics.BoxSize, precision: 6);
        Assert.Equal(metrics.BoxCenter.X, metrics.CheckCenter.X, tolerance);
        Assert.Equal(metrics.BoxCenter.Y, metrics.CheckCenter.Y, tolerance);
        Assert.Equal(12 * dpiScale, metrics.CheckBounds.Width, precision: 6);
        Assert.Equal(8 * dpiScale, metrics.CheckBounds.Height, precision: 6);
        Assert.Equal(AxisToggleMetrics.CheckStrokeThickness * dpiScale, metrics.CheckStrokeThickness, precision: 6);
    }
}
