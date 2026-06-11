using System.Linq;
using SerialPlot.CsvGen;
using Xunit;

namespace SerialPlot.Tests.CsvGen;

public sealed class CsvGenOptionsParserTests
{
    [Fact]
    public void DefaultsProvideUsefulLiveChannels()
    {
        var options = CsvGenOptionsParser.Parse([]);

        Assert.Equal(100d, options.RateHz);
        Assert.True(options.Realtime);
        Assert.Null(options.EffectiveSampleCount());
        Assert.Equal(["t", "sine", "noise"], options.Channels.Select(x => x.Name).ToArray());
    }

    [Fact]
    public void ParsesRepeatedChannelSpecsAndFiniteSamples()
    {
        var options = CsvGenOptionsParser.Parse([
            "--rate", "50",
            "--samples", "12",
            "--channel", "t:time",
            "--channel", "volts:sine:freq=2:amp=3:offset=1",
            "--no-realtime",
        ]);

        Assert.Equal(50d, options.RateHz);
        Assert.Equal(12, options.EffectiveSampleCount());
        Assert.False(options.Realtime);
        Assert.Equal(ChannelKind.Sine, options.Channels[1].Kind);
        Assert.Equal(2d, options.Channels[1].Frequency);
        Assert.Equal(3d, options.Channels[1].Amplitude);
        Assert.Equal(1d, options.Channels[1].Offset);
    }

    [Fact]
    public void RejectsDuplicateNamesAndConflictingLimits()
    {
        Assert.Throws<CsvGenConfigurationException>(() => CsvGenOptionsParser.Parse([
            "--channel", "a:sine",
            "--channel", "a:noise",
        ]));

        Assert.Throws<CsvGenConfigurationException>(() => CsvGenOptionsParser.Parse([
            "--samples", "1",
            "--duration", "1",
        ]));
    }

    [Fact]
    public void ParsesTcpListenPort()
    {
        var options = CsvGenOptionsParser.Parse(["--tcp-listen", "5001"]);

        Assert.Equal(5001, options.TcpListenPort);
    }
}
