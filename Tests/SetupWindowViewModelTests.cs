using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using SerialPlot.Models;
using SerialPlot.Services;
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

    [Fact]
    public void RecentHistoryPrefillsLastUsedSourceAndMostRecentEntry()
    {
        var history = RecentSetupHistory.Empty
            .Remember(UdpConfig("imu-old", 55123), new System.DateTimeOffset(2026, 1, 1, 0, 0, 0, System.TimeSpan.Zero))
            .Remember(UdpConfig("imu-new", 55124), new System.DateTimeOffset(2026, 1, 2, 0, 0, 0, System.TimeSpan.Zero));

        var vm = new SetupWindowViewModel(null, history);

        Assert.Equal(SourceType.Udp, vm.Source);
        Assert.Equal("imu-new", vm.SourceName);
        Assert.Equal(55124, vm.Port);
        Assert.Equal("poll", vm.UdpMessage);
        Assert.Equal(2, vm.RecentSetups.Count);
    }

    [Fact]
    public void RecentHistoryDropdownIsFilteredBySourceType()
    {
        var history = RecentSetupHistory.Empty
            .Remember(UdpConfig("imu", 55123), new System.DateTimeOffset(2026, 1, 1, 0, 0, 0, System.TimeSpan.Zero))
            .Remember(SerialConfig("gps", "/dev/ttyUSB0"), new System.DateTimeOffset(2026, 1, 2, 0, 0, 0, System.TimeSpan.Zero));
        var vm = new SetupWindowViewModel(null, history);

        vm.Source = SourceType.Udp;

        var recent = Assert.Single(vm.RecentSetups);
        Assert.Equal(SourceType.Udp, recent.Config.Source);
    }

    [Fact]
    public void SelectingRecentEntryAppliesAllSetupFields()
    {
        var history = RecentSetupHistory.Empty
            .Remember(UdpConfig("imu", 55123), new System.DateTimeOffset(2026, 1, 1, 0, 0, 0, System.TimeSpan.Zero))
            .Remember(UdpConfig("weather", 55124), new System.DateTimeOffset(2026, 1, 2, 0, 0, 0, System.TimeSpan.Zero));
        var vm = new SetupWindowViewModel(null, history)
        {
            Source = SourceType.Udp,
        };

        vm.SelectedRecentSetup = vm.RecentSetups.First(x => x.Config.Name == "imu");

        Assert.Equal("imu", vm.SourceName);
        Assert.Equal(55123, vm.Port);
        Assert.Equal("time", vm.InitialX);
        Assert.Equal("ax", vm.InitialYLeft);
        Assert.Equal("lat", vm.InitialYRight);
        Assert.Equal(5, vm.UdpResendIntervalSeconds);
    }

    [Fact]
    public void InitialConfigIgnoresRecentHistory()
    {
        var history = RecentSetupHistory.Empty
            .Remember(UdpConfig("recent", 55123), new System.DateTimeOffset(2026, 1, 1, 0, 0, 0, System.TimeSpan.Zero));
        var cliConfig = new AppConfig(
            SourceType.Serial,
            "COM7",
            9600,
            null,
            null,
            null,
            null,
            2000,
            TimestampUnit.Seconds,
            null,
            [],
            []);

        var vm = new SetupWindowViewModel(cliConfig, history);

        Assert.Equal(SourceType.Serial, vm.Source);
        Assert.Equal("COM7", vm.SerialPort);
        Assert.Equal(9600, vm.Baud);
        Assert.Empty(vm.RecentSetups);
    }

    private static InputSourceConfig UdpConfig(string name, int port) => new(
        name,
        SourceType.Udp,
        null,
        null,
        "127.0.0.1",
        port,
        "poll",
        5,
        1234,
        TimestampUnit.Milliseconds,
        "time",
        ["ax"],
        ["lat"]);

    private static InputSourceConfig SerialConfig(string name, string port) => new(
        name,
        SourceType.Serial,
        port,
        115200,
        null,
        null,
        null,
        null,
        4321,
        TimestampUnit.Auto,
        null,
        [],
        []);
}
