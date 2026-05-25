using SerialPlot.Models;
using SerialPlot.Services;
using Xunit;

namespace SerialPlot.Tests;

public sealed class CliConfigParserTests
{
    [Fact]
    public void NoArgsRequestsSetup()
    {
        var result = CliConfigParser.Parse([]);

        Assert.False(result.IsComplete);
        Assert.False(result.HadAnyArgs);
        Assert.Equal(AppConfig.DefaultBufferSize, result.Config.BufferSize);
    }

    [Fact]
    public void NoArgsWithRedirectedStdinStartsStdinSource()
    {
        var result = CliConfigParser.Parse([], stdinRedirected: true);

        Assert.True(result.IsComplete);
        Assert.False(result.HadAnyArgs);
        Assert.Equal(SourceType.Stdin, result.Config.Source);
    }

    [Fact]
    public void StdinArgsAreComplete()
    {
        var result = CliConfigParser.Parse(["--source", "stdin", "--buffer-size", "12", "--timestamp-unit", "milliseconds"]);

        Assert.True(result.IsComplete);
        Assert.Equal(SourceType.Stdin, result.Config.Source);
        Assert.Equal(12, result.Config.BufferSize);
        Assert.Equal(TimestampUnit.Milliseconds, result.Config.TimestampUnit);
    }

    [Fact]
    public void SerialRequiresPortAndBaud()
    {
        var result = CliConfigParser.Parse(["--source", "serial", "--serial-port", "COM3"]);

        Assert.False(result.IsComplete);
        Assert.Contains("baud", result.Error);
    }

    [Fact]
    public void TcpRequiresHostAndPort()
    {
        var result = CliConfigParser.Parse(["--source", "tcp", "--host", "localhost"]);

        Assert.False(result.IsComplete);
        Assert.Contains("port", result.Error);
    }
}
