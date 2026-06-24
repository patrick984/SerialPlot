using System.Collections.Generic;
using System.ComponentModel;
using SerialPlot.Models;
using SerialPlot.ViewModels;
using Xunit;

namespace SerialPlot.Tests;

public sealed class SetupWindowViewModelTests
{
    [Theory]
    [InlineData(SourceType.Stdin, false, false, false)]
    [InlineData(SourceType.Serial, true, false, false)]
    [InlineData(SourceType.Tcp, false, true, false)]
    [InlineData(SourceType.Udp, false, true, true)]
    [InlineData(SourceType.Test, false, false, false)]
    public void VisibilityFlagsFollowSelectedSource(SourceType source, bool showSerial, bool showNetwork, bool showUdpMessage)
    {
        var vm = new SetupWindowViewModel { Source = source };

        Assert.Equal(showSerial, vm.ShowSerialSettings);
        Assert.Equal(showNetwork, vm.ShowNetworkSettings);
        Assert.Equal(showUdpMessage, vm.ShowUdpMessage);
    }

    [Fact]
    public void ChangingSourceRaisesVisibilityNotifications()
    {
        var vm = new SetupWindowViewModel();
        var changed = new List<string?>();
        vm.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        vm.Source = SourceType.Udp;

        Assert.Contains(nameof(SetupWindowViewModel.ShowSerialSettings), changed);
        Assert.Contains(nameof(SetupWindowViewModel.ShowNetworkSettings), changed);
        Assert.Contains(nameof(SetupWindowViewModel.ShowUdpMessage), changed);
    }

    [Fact]
    public void ToConfigMapsSelectedSourceFields()
    {
        var vm = new SetupWindowViewModel
        {
            SourceName = "imu",
            Source = SourceType.Udp,
            Host = "127.0.0.1",
            Port = 55123,
            UdpMessage = "poll",
            UdpResendIntervalSeconds = 7,
            Baud = 115200,
            BufferSize = 1234,
            TimestampUnit = TimestampUnit.Milliseconds,
            InitialX = "time",
            InitialYLeft = "ax, ay",
            InitialYRight = "lat",
        };

        var config = vm.ToConfig();

        Assert.Equal("imu", config.Sources[0].Name);
        Assert.Equal(SourceType.Udp, config.Sources[0].Source);
        Assert.Equal("127.0.0.1", config.Sources[0].Host);
        Assert.Equal(55123, config.Sources[0].Port);
        Assert.Equal("poll", config.Sources[0].UdpMessage);
        Assert.Equal(7, config.Sources[0].UdpResendIntervalSeconds);
        Assert.Equal(1234, config.Sources[0].BufferSize);
        Assert.Equal(TimestampUnit.Milliseconds, config.Sources[0].TimestampUnit);
        Assert.Equal("time", config.Sources[0].InitialX);
        Assert.Equal(["ax", "ay"], config.Sources[0].InitialYLeft);
        Assert.Equal(["lat"], config.Sources[0].InitialYRight);
    }
}
