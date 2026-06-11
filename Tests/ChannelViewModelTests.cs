using Avalonia.Media;
using SerialPlot.ViewModels;
using Xunit;

namespace SerialPlot.Tests;

public sealed class ChannelViewModelTests
{
    [Fact]
    public void TraceBrushesCanBeAssignedAndClearedPerAxis()
    {
        var channel = new ChannelViewModel("temperature", 1);
        var left = Brushes.Red;
        var right = Brushes.Blue;

        channel.LeftTraceBrush = left;
        channel.RightTraceBrush = right;

        Assert.Same(left, channel.LeftTraceBrush);
        Assert.Same(right, channel.RightTraceBrush);

        channel.LeftTraceBrush = null;
        channel.RightTraceBrush = null;

        Assert.Null(channel.LeftTraceBrush);
        Assert.Null(channel.RightTraceBrush);
    }
}
