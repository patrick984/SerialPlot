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
    public void UdpArgsParseResendInterval()
    {
        var result = CliConfigParser.Parse([
            "--source", "udp",
            "--host", "127.0.0.1",
            "--port", "5000",
            "--udp-message", "poll",
            "--udp-resend-interval", "7",
        ]);

        Assert.True(result.IsComplete);
        Assert.Equal(SourceType.Udp, result.Config.Source);
        Assert.Equal(7, result.Config.UdpResendIntervalSeconds);
        Assert.Equal(7, result.Config.Sources[0].UdpResendIntervalSeconds);
    }

    [Fact]
    public void ZeroUdpResendIntervalDisablesResend()
    {
        var result = CliConfigParser.Parse([
            "--source", "udp",
            "--host", "127.0.0.1",
            "--port", "5000",
            "--udp-message", "poll",
            "--udp-resend-interval", "0",
        ]);

        Assert.True(result.IsComplete);
        Assert.Null(result.Config.UdpResendIntervalSeconds);
        Assert.Null(result.Config.Sources[0].UdpResendIntervalSeconds);
    }

    [Fact]
    public void NegativeUdpResendIntervalIsInvalid()
    {
        var result = CliConfigParser.Parse([
            "--source", "udp",
            "--host", "127.0.0.1",
            "--port", "5000",
            "--udp-message", "poll",
            "--udp-resend-interval", "-1",
        ]);

        Assert.False(result.IsComplete);
        Assert.Contains("udp-resend-interval", result.Error);
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

    [Fact]
    public void TestSourceRequiresNoConnectionSettings()
    {
        var result = CliConfigParser.Parse(["--source", "test"]);

        Assert.True(result.IsComplete);
        Assert.Equal(SourceType.Test, result.Config.Source);
        Assert.Equal(SourceType.Test, result.Config.Sources[0].Source);
    }

    [Fact]
    public void RepeatSourceSpecsCreateIndependentSources()
    {
        var result = CliConfigParser.Parse([
            "--source-spec", "name=imu;type=serial;serial-port=COM3;baud=115200;buffer-size=10;x=t;y-left=ax,ay",
            "--source-spec", "name=gps;type=udp;host=127.0.0.1;port=5000;udp-message=poll;udp-resend-interval=3;x=time;y-right=lat",
            "--source-spec", "name=test;type=test;x=time;y-left=sine",
        ]);

        Assert.True(result.IsComplete);
        Assert.Equal(3, result.Config.Sources.Count);
        Assert.Equal("imu", result.Config.Sources[0].Name);
        Assert.Equal(SourceType.Serial, result.Config.Sources[0].Source);
        Assert.Equal(["ax", "ay"], result.Config.Sources[0].InitialYLeft);
        Assert.Equal("gps", result.Config.Sources[1].Name);
        Assert.Equal(SourceType.Udp, result.Config.Sources[1].Source);
        Assert.Equal(3, result.Config.Sources[1].UdpResendIntervalSeconds);
        Assert.Equal(["lat"], result.Config.Sources[1].InitialYRight);
        Assert.Equal(SourceType.Test, result.Config.Sources[2].Source);
    }

    [Fact]
    public void InvalidSourceSpecReportsPerSourceValidation()
    {
        var result = CliConfigParser.Parse(["--source-spec", "name=bad;type=tcp;host=localhost"]);

        Assert.False(result.IsComplete);
        Assert.Contains("bad", result.Error);
        Assert.Contains("port", result.Error);
    }
}
