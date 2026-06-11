using System;
using System.IO;
using System.Linq;
using SerialPlot.Models;
using SerialPlot.Services;
using SerialPlot.ViewModels;
using Xunit;

namespace SerialPlot.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void XAutoscaleModeOptionsExposeFriendlyLabels()
    {
        var vm = CreateViewModel();

        Assert.Equal(
            ["Continuous Follow", "Stepped Expand", "Stepped Pan"],
            vm.XAutoscaleModeOptions.Select(x => x.Label).ToArray());
    }

    [Fact]
    public void SelectingXAutoscaleModeOptionUpdatesMode()
    {
        var vm = CreateViewModel();

        vm.SelectedXAutoscaleModeOption = vm.XAutoscaleModeOptions.Single(x => x.Mode == XAutoscaleMode.SteppedPan);

        Assert.Equal(XAutoscaleMode.SteppedPan, vm.XAutoscaleMode);
    }

    [Fact]
    public void SettingXAutoscaleModeUpdatesSelectedOption()
    {
        var vm = CreateViewModel();

        vm.XAutoscaleMode = XAutoscaleMode.SteppedPan;

        Assert.Equal("Stepped Pan", vm.SelectedXAutoscaleModeOption.Label);
    }

    [Fact]
    public void FutureSpaceControlIsEnabledOnlyForSteppedExpansion()
    {
        var vm = CreateViewModel();

        vm.XAutoscaleMode = XAutoscaleMode.SteppedExpansion;
        Assert.True(vm.IsSteppedExpansionSelected);

        vm.XAutoscaleMode = XAutoscaleMode.SteppedPan;
        Assert.False(vm.IsSteppedExpansionSelected);
    }

    [Fact]
    public void AddSourceSelectsAndExposesIndependentChannelCollection()
    {
        var vm = CreateViewModel();
        var added = new InputSourceViewModel(SourceConfig("second"), new TextReaderLineSource(TextReader.Null));

        vm.AddSource(added);

        Assert.Equal(2, vm.Sources.Count);
        Assert.Same(added, vm.SelectedSource);
        Assert.Same(added.Channels, vm.Channels);
    }

    [Fact]
    public void SelectedTracesIncludeSourceIdentity()
    {
        var sourceA = new InputSourceViewModel(SourceConfig("a"), new TextReaderLineSource(TextReader.Null));
        var sourceB = new InputSourceViewModel(SourceConfig("b"), new TextReaderLineSource(TextReader.Null));
        var vm = new MainWindowViewModel(
            AppConfig.Defaults(),
            [sourceA, sourceB],
            new UserPreferencesService(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "preferences.json")));
        var channelA = new ChannelViewModel("value", 1) { CanBeY = true, IsSelectedLeft = true };
        var channelB = new ChannelViewModel("value", 1) { CanBeY = true, IsSelectedRight = true };
        sourceA.Channels.Add(channelA);
        sourceB.Channels.Add(channelB);

        var traces = vm.SelectedTraces;

        Assert.Contains(traces, x => ReferenceEquals(x.Source, sourceA) && x.Channel == channelA && x.Side == TraceAxisSide.Left);
        Assert.Contains(traces, x => ReferenceEquals(x.Source, sourceB) && x.Channel == channelB && x.Side == TraceAxisSide.Right);
    }

    private static MainWindowViewModel CreateViewModel()
        => new(
            AppConfig.Defaults(),
            new TextReaderLineSource(TextReader.Null),
            new UserPreferencesService(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "preferences.json")));

    private static InputSourceConfig SourceConfig(string name)
        => new(
            name,
            SourceType.Stdin,
            null,
            null,
            null,
            null,
            null,
            AppConfig.DefaultBufferSize,
            TimestampUnit.Auto,
            null,
            [],
            []);
}
